using EtlService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EtlService.Application.Services;

public class BackfillService
{
    private readonly IExtractService _stockDataService;
    private readonly IDownloadTracker _tracker;
    private readonly ICsvExporter _csvExporter;
    private readonly ILogger<BackfillService> _logger;
    public BackfillService(
        IExtractService stockDataService,
        IDownloadTracker tracker,
        ICsvExporter csvExporter,
        ILogger<BackfillService> logger)
    {
        _stockDataService = stockDataService;
        _tracker = tracker;
        _csvExporter = csvExporter;
        _logger = logger;
    }

    public async Task BackfillLast30DaysAsync(string symbol, string interval)
    {
        try
        {
            _logger.LogInformation("Backfill started for {Symbol}", symbol);
            // Fetch once from Alpha Vantage (returns full intraday data for past 30 days)
            var allData = await _stockDataService.ExtractDailyStockData(symbol, interval);

            if (allData.IsError)
            {
                _logger.LogError("Error extracting data: {Error}", allData.FirstError.Description);
                return;
            }

            if (!allData.Value.Any())
            {
                _logger.LogWarning("No data returned for {Symbol}", symbol);
                return;
            }

            // Group data by date
            var groupedByDate = allData.Value
                .Values
                .GroupBy(r => DateTime.Parse(r.Date).Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            for (int i = 1; i <= 30; i++)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);

                if (_tracker.HasAlreadyDownloaded(symbol, date, interval))
                    continue;

                if (!groupedByDate.TryGetValue(date, out var dailyData) || dailyData.Count == 0)
                    continue;

                _csvExporter.SaveToCsv(dailyData, symbol, date);
                _tracker.MarkAsDownloaded(symbol, date, interval);

                await Task.Delay(15000);
            }
            _logger.LogInformation("Backfill finished for {Symbol}", symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during backfill for {Symbol}", symbol);
        }

        finally
        {
            _logger.LogInformation("Backfill process completed for {Symbol}", symbol);
        }
    }
}