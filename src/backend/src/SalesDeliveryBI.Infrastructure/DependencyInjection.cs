using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Infrastructure.Caching;
using SalesDeliveryBI.Infrastructure.Jobs;
using SalesDeliveryBI.Infrastructure.Persistence.Dapper;
using SalesDeliveryBI.Infrastructure.Persistence.EfCore;
using SalesDeliveryBI.Infrastructure.Persistence.EfCore.Seed;
using SalesDeliveryBI.Infrastructure.Security;
using StackExchange.Redis;

namespace SalesDeliveryBI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IUnitAccessGuard, UnitAccessGuard>();
        services.AddQuotationAuthorizationPolicies();

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            string connectionString = configuration.GetConnectionString("SalesDeliveryBi")
                ?? throw new InvalidOperationException("Missing 'ConnectionStrings:SalesDeliveryBi' configuration.");

            options.UseNpgsql(connectionString)
                .AddInterceptors(provider.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddScoped<DatabaseSeeder>();

        services.AddSingleton<DapperContext>();
        services.AddScoped<IQuotationRepository, QuotationRepository>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            string redisConnectionString = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Redis' configuration.");

            return ConnectionMultiplexer.Connect(redisConnectionString);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddCacheWarmupJobs();

        return services;
    }
}
