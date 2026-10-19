using Gold.Application.Abstractions;
using Gold.Infrastructure.Persistence;
using Gold.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gold.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Database:Provider"] ?? "Sqlite";
        var cs = config.GetConnectionString("Default") ?? "Data Source=gold.db";

        services.AddDbContext<AppDbContext>(o =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)) o.UseSqlServer(cs);
            else o.UseSqlite(cs);
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IClock, SystemClock>();

        var hashKey = config["Security:HashKey"];
        if (string.IsNullOrWhiteSpace(hashKey) || hashKey.Length < 32)
            throw new InvalidOperationException("Security:HashKey is missing or shorter than 32 chars (set it via User Secrets / env var).");
        services.AddSingleton<ISecretHasher>(new HmacSecretHasher(hashKey));

        services.AddSingleton<ISmsSender, LogSmsSender>();       // TODO prod: Kavenegar / sms.ir adapter
        services.AddSingleton<IPaymentGateway, FakePaymentGateway>(); // TODO prod: Parbad adapter
        services.AddScoped<DataSeeder>();
        services.AddSingleton(new LocalMediaStorage(config["Media:Root"] ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "media")));
        return services;
    }
}
