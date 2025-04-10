using EtlService.Application.Configuration;
using EtlService.Application.Interfaces;
using EtlService.Infrastructure.BackgroundJobs;
using EtlService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtlService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {        
        services.Configure<AlphaVantageOptions>(configuration.GetSection("AlphaVantage"));
        services.Configure<DownloadTrackerOptions>(configuration.GetSection("DownloadTracking")); 
        services.Configure<CsvExportOptions>(configuration.GetSection("CsvExport"));
        services.AddHttpClient<AlphaVantageOptions>(client =>
        {
            var apiKey = configuration["AlphaVantage:ApiKey"];
            client.BaseAddress = new Uri($"https://www.alphavantage.co/query?apikey={apiKey}");
        });
        services.AddHttpClient<IExtractService, ExtractService>();
        services.AddScoped<IExtractService, ExtractService>();
        services.AddScoped<ICsvExporter, CsvExporterService>();
        services.AddScoped<IDownloadTracker, JsonDownloadTracker>();

        services.Configure<ServiceBusOptions>(configuration.GetSection("AzureServiceBus"));
        services.AddSingleton<IMessagePublisher, ServiceBusMessagePublisher>();

        services.AddHostedService<BackfillJob>();
        services.AddHostedService<EtlQueueConsumer>();

        // Other infrastructure services like repositories, file storage, etc.

        return services;
    }
}
