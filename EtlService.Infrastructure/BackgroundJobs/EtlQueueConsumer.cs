using Azure.Messaging.ServiceBus;
using EtlService.Application.Configuration;
using EtlService.Application.Interfaces;
using EtlService.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EtlService.Infrastructure.BackgroundJobs;

public class EtlQueueConsumer : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger<EtlQueueConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public EtlQueueConsumer(
        IOptions<ServiceBusOptions> options,
        ILogger<EtlQueueConsumer> logger,
    IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        var client = new ServiceBusClient(options.Value.ConnectionString);
        _processor = client.CreateProcessor(options.Value.QueueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
        });
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ErrorHandlerAsync;

        return _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var extractService = scope.ServiceProvider.GetRequiredService<IExtractService>();
            var csvExporter = scope.ServiceProvider.GetRequiredService<ICsvExporter>();
            var tracker = scope.ServiceProvider.GetRequiredService<IDownloadTracker>();

            var messageBody = args.Message.Body.ToString();
            var etlRequest = JsonSerializer.Deserialize<EtlRequest>(messageBody);

            if (etlRequest == null)
            {
                _logger.LogWarning("Received null or invalid ETL request message.");
                await args.DeadLetterMessageAsync(args.Message, "Invalid ETLRequest payload");
                return;
            }

            var date = etlRequest.RequestedDate.Date;

            if (tracker.HasAlreadyDownloaded(etlRequest.Symbol, date, etlRequest.Interval))
            {
                _logger.LogInformation("Skipping duplicate ETL request for {Symbol} on {Date}", etlRequest.Symbol, date);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            var allRecords = await extractService.ExtractDailyStockData(etlRequest.Symbol, etlRequest.Interval);

            if (allRecords.IsError || allRecords.Value == null || !allRecords.Value.Any())
            {
                _logger.LogWarning("No data found for {Symbol} on {Date}", etlRequest.Symbol, date);
                await args.DeadLetterMessageAsync(args.Message, "No data found or extraction failed");
                return;
            }

            var filtered = allRecords.Value
                .Where(r => DateTime.Parse(r.Key).Date == date)
                .ToDictionary(r => r.Key, r => r.Value);

            if (!filtered.Any())
            {
                _logger.LogWarning("No intraday data found for {Symbol} on {Date}", etlRequest.Symbol, date);
                await args.DeadLetterMessageAsync(args.Message, "Filtered data empty");
                return;
            }

            csvExporter.SaveToCsv(filtered.Values, etlRequest.Symbol, date);
            tracker.MarkAsDownloaded(etlRequest.Symbol, date, etlRequest.Interval);

            _logger.LogInformation("ETL complete for {Symbol} on {Date}", etlRequest.Symbol, date);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ETL message: {Message}", ex.Message);

            int maxRetries = 3;
            if (args.Message.DeliveryCount >= maxRetries)
            {
                _logger.LogWarning("Max retry limit reached. Moving message to dead-letter queue.");
                await args.DeadLetterMessageAsync(args.Message, "Max retry exceeded", ex.Message);
            }
            else
            {
                _logger.LogInformation("Retrying message. Current count: {Count}", args.Message.DeliveryCount);
                await args.AbandonMessageAsync(args.Message);
            }
        }
    }

    private Task ErrorHandlerAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus error: {Message}", args.Exception.Message);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
