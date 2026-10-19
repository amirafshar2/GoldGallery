using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Gold.Api.Endpoints;
using Gold.Api.Infrastructure;
using Gold.Api.Realtime;
using Gold.Api.Security;
using Gold.Api.Workers;
using Gold.Application;
using Gold.Application.Abstractions;
using Gold.Domain.Entities;
using Gold.Infrastructure;
using Gold.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

// ---------- fail-fast configuration checks ----------
// Showcase (public demo) only: if the host did not provide secrets, generate random ones for this process.
// Nothing real is protected there (sample data, sandbox payments); after a restart everyone simply logs in again.
// Real installs (Production) still fail fast without explicit keys.
if (builder.Configuration.GetValue<bool>("Showcase:Enabled"))
{
    static string Random64() => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
    if ((builder.Configuration["Jwt:Key"] ?? "").Length < 32) builder.Configuration["Jwt:Key"] = Random64();
    if ((builder.Configuration["Security:HashKey"] ?? "").Length < 32) builder.Configuration["Security:HashKey"] = Random64();
}
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 chars (set via User Secrets / env var Jwt__Key).");

var demoLogin = builder.Configuration.GetValue<bool>("DemoLogin:Enabled");
// Showcase = a PUBLIC DEMO for presenting the site to a shop: demo logins, sandbox payments, sample data,
// auto-reset. It never takes real money (only the fake gateway exists) and the UI shows a "demo" ribbon.
var showcase = builder.Configuration.GetValue<bool>("Showcase:Enabled");
if (demoLogin && builder.Environment.IsProduction() && !showcase)
    throw new InvalidOperationException("DemoLogin:Enabled must never be true in Production (only with Showcase:Enabled for a public demo).");
var devLike = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing") || showcase;

// ---------- services ----------
builder.Services.AddApplication();
builder.Services.AddSingleton(new Gold.Domain.Pricing.RateGuard(
    TimeSpan.FromMinutes(builder.Configuration.GetValue("RateGuard:MaxAgeMinutes", 30)),
    Gold.Domain.ValueObjects.Percentage.FromPercent(builder.Configuration.GetValue("RateGuard:MaxJumpPercent", 5m))));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IRateNotifier, SignalRRateNotifier>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
ValidatorOptions.Global.LanguageManager.Culture = new System.Globalization.CultureInfo("fa");
ValidatorOptions.Global.LanguageManager.Enabled = true;
builder.Services.AddHybridCache();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = c =>
    c.ProblemDetails.Extensions["traceId"] = c.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("db", tags: ["ready"]);
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
});
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<OrderExpiryWorker>();
    builder.Services.AddHostedService<OutboxDispatcher>();
}
if (showcase) builder.Services.AddHostedService<ShowcaseResetWorker>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ClockSkew = TimeSpan.FromSeconds(30),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = "sub",
    };
    // SignalR: JWT via query string only for the hub (and masked in logs).
    o.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (ctx.HttpContext.Request.Path.StartsWithSegments("/hubs")) ctx.Token = ctx.Request.Query["access_token"];
            return Task.CompletedTask;
        },
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Customer, p => p.RequireAuthenticatedUser())
    .AddPolicy(Policies.Staff, p => p.RequireRole(nameof(UserRole.Staff), nameof(UserRole.Admin)))
    .AddPolicy(Policies.Admin, p => p.RequireRole(nameof(UserRole.Admin)))
    .AddPolicy(Policies.RealAdmin, p => p.RequireRole(nameof(UserRole.Admin)).RequireClaim("demo", "0"));

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy("web", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var limits = builder.Configuration.GetSection("RateLimits");
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    string Ip(HttpContext c) => c.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    o.AddPolicy("otp", c => RateLimitPartition.GetFixedWindowLimiter("otp:" + Ip(c), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = limits.GetValue("Otp", 5), Window = TimeSpan.FromMinutes(2) }));
    o.AddPolicy("auth", c => RateLimitPartition.GetFixedWindowLimiter("auth:" + Ip(c), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = limits.GetValue("Auth", 20), Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("payment", c => RateLimitPartition.GetFixedWindowLimiter("pay:" + Ip(c), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = limits.GetValue("Payment", 20), Window = TimeSpan.FromMinutes(1) }));
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(c => RateLimitPartition.GetFixedWindowLimiter(
        "global:" + Ip(c), _ => new FixedWindowRateLimiterOptions { PermitLimit = limits.GetValue("Global", 300), Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

// ---------- pipeline ----------
app.UseExceptionHandler();
app.UseResponseCompression();
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Content-Security-Policy"] = ctx.Request.Path.StartsWithSegments("/swagger") || ctx.Request.Path.StartsWithSegments("/dev") || ctx.Request.Path.StartsWithSegments("/media")
        ? "default-src 'self' 'unsafe-inline' data:"
        : "default-src 'none'; frame-ancestors 'none'";
    await next();
});
if (!devLike) { app.UseHsts(); app.UseHttpsRedirection(); } // showcase: TLS ends at the host (Render), API is internal
app.UseSerilogRequestLogging(); // logs RequestPath only — query strings (tokens) are never logged
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (devLike)
{
    // Production uses reviewed migration scripts, never automatic schema changes.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DevDatabase.EnsureCurrentAsync(db, app.Logger);
    if (!string.Equals(app.Configuration["Seed:Mode"], "Empty", StringComparison.OrdinalIgnoreCase))
        await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(app.Configuration["Seed:Profile"]);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => { o.SwaggerEndpoint("/openapi/v1.json", "Gold API v1"); o.DocumentTitle = "Gold API"; });
    app.MapFakeGateway();
}
else if (showcase) app.MapFakeGateway(exposeOtp: false);

var api = app.MapGroup("/api/v1");
api.MapAuth(demoLogin);
api.MapCatalog();
api.MapOrders();
api.MapSite(app.Configuration);
app.MapMedia();
app.MapHub<RatesHub>("/hubs/rates");
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });

app.Run();

public partial class Program;
