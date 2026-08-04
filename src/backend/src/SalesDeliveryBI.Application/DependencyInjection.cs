using Microsoft.Extensions.DependencyInjection;
using SalesDeliveryBI.Application.Services;

namespace SalesDeliveryBI.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Application-layer services only. IQuotationRepository/ICacheService/IUnitAccessGuard/
    /// ICurrentUserContext have no implementation here — Infrastructure's DependencyInjection.cs binds them.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<QuotationAppService>();
        services.AddScoped<AuthAppService>();
        services.AddScoped<AdminAppService>();

        return services;
    }
}
