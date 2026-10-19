using Gold.Application.Auth;
using Gold.Application.Catalog;
using Gold.Application.Orders;
using Gold.Application.Payments;
using Gold.Application.Rates;
using Gold.Domain.Pricing;
using Microsoft.Extensions.DependencyInjection;

namespace Gold.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // RateGuard is registered by the host from configuration (RateGuard:MaxAgeMinutes, RateGuard:MaxJumpPercent).
        services.AddScoped<RateService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<AuthService>();
        services.AddScoped<OrderService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<Site.SiteService>();
        return services;
    }
}
