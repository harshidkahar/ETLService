using ErrorOr;
using EtlService.Application.Interfaces;
using EtlService.Application.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EtlService.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class EtlController : ControllerBase
    {
        private readonly IExtractService _extractService;
        private readonly ICsvExporter _csvExporter;
        private readonly IConfiguration _config;
        private readonly IDownloadTracker _tracker;
        private readonly ILogger<EtlController> _logger;
        private readonly IMessagePublisher _publisher;
        public EtlController(IExtractService extractService, ICsvExporter csvExporter, IConfiguration config, IDownloadTracker tracker, ILogger<EtlController> logger, IMessagePublisher publisher)
        {
            _extractService = extractService;
            _csvExporter = csvExporter;
            _config = config;
            _tracker = tracker;
            _logger = logger;
            _publisher = publisher;
        }

        [HttpPost("{symbol}")]
        public async Task<IActionResult> RunEtl(string symbol)
        {
            try
            {
                _logger.LogInformation("ETL started for {Symbol}", symbol);
                string interval = _config["AlphaVantage:Interval"]!;
                DateTime currentDate = DateTime.UtcNow;

                if (_tracker.HasAlreadyDownloaded(symbol, currentDate, interval))
                {
                    return Conflict(new
                    {
                        Message = $"Data for {symbol} on {currentDate:yyyy-MM-dd} already processed."
                    });
                }

                var allRecords = await _extractService.ExtractDailyStockData(symbol, interval);

                if (allRecords.IsError)
                {
                    _logger.LogError("ETL failed for {Symbol} on {Date}: {Error}", symbol, currentDate, allRecords.FirstError.Description);
                    return Problem(allRecords.FirstError.ToString());
                }

                if (allRecords.Value == null || !allRecords.Value.Any())
                {
                    _logger.LogError("ETL finished: No data returned for {Symbol} on {Date}", symbol, currentDate);
                    return NotFound(new { Message = "No data returned from Alpha Vantage." });
                }

                // Filter to include only records for the current date
                var filteredRecords = allRecords.Value
                    .Where(r => DateTime.Parse(r.Key).Date == currentDate)
                    .ToDictionary(r => r.Key, r => r.Value);

                if (!filteredRecords.Any())
                {
                    return NotFound(new { Message = "No intraday stock data available for the current date." });
                }

                string filePath = _csvExporter.SaveToCsv(filteredRecords.Values, symbol, currentDate);

                _tracker.MarkAsDownloaded(symbol, currentDate, interval);

                if (filePath == "NoData")
                {
                    _logger.LogError("ETL finished: No data to export.");
                    return NotFound(new { Message = "No data to export." });
                }

                _logger.LogInformation("ETL complete for {Symbol} on {Date}", symbol, currentDate);

                return Ok(new
                {
                    Message = "ETL complete",
                    Symbol = symbol,
                    Date = currentDate.ToString("yyyy-MM-dd"),
                    FilePath = filePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ETL failed: {Message}", ex.Message);
                return StatusCode(500, new { Message = "An error occurred during ETL", Error = ex.Message });
            }
        }


        [HttpPost("queue/{symbol}")]
        public async Task<IActionResult> QueueEtl(string symbol)
        {
            try
            {
                _logger.LogInformation("ETL queue request received for {Symbol}", symbol);

                string interval = _config["AlphaVantage:Interval"] ?? "15min";
                DateTime currentDate = DateTime.UtcNow;

                var message = new EtlRequest
                {
                    Symbol = symbol,
                    Interval = interval,
                    RequestedDate = currentDate
                };

                var messageJson = JsonSerializer.Serialize(message);
                await _publisher.PublishAsync(messageJson);

                _logger.LogInformation("ETL request for {Symbol} published to queue", symbol);

                return Accepted(new
                {
                    Message = "ETL request queued successfully",
                    Symbol = symbol,
                    Interval = interval,
                    RequestedDate = currentDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ETL queue publish failed: {Message}", ex.Message);
                return StatusCode(500, new { Message = "An error occurred while queuing the ETL request", Error = ex.Message });
            }
        }
    }
}
