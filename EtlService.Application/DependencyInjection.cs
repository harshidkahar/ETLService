using EtlService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EtlService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<BackfillService>();

        return services;
    }
}
