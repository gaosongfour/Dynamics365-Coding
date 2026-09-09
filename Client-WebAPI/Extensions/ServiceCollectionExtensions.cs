using Crm.ClientWebAPI.Services;

namespace Crm.ClientWebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrmServices(this IServiceCollection services)
    {
        services.AddSingleton<CrmServiceManager>();
        services.AddScoped<CrmOperationsHelper>();
        return services;
    }
}
