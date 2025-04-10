using ErrorOr;
using EtlService.Application.Common.Errors;
using EtlService.Application.Configuration;
using EtlService.Application.Interfaces;
using EtlService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EtlService.Infrastructure.Services
{
    public class ExtractService : IExtractService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        public ExtractService(HttpClient httpClient, IOptions<AlphaVantageOptions> options)
        {
            _httpClient = httpClient;
            _apiKey = options.Value.ApiKey;
        }

        public async Task<ErrorOr<Dictionary<string, StockRecord>>> ExtractDailyStockData(string symbol, string interval)
        {
            try
            {
                if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(interval))
                    return Error.Failure("InvalidInput", "Symbol or interval cannot be null or empty.");

                string function = "TIME_SERIES_INTRADAY";
                string url = $"https://www.alphavantage.co/query?function={function}&symbol={symbol}&interval={interval}&outputsize=full&apikey={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(responseStream);

                var records = new Dictionary<string, StockRecord>();
                if (!doc.RootElement.TryGetProperty($"Time Series ({interval})", out JsonElement timeSeries))
                {
                    return ErrorCodes.Extract.NoTimeSeries;
                }
                if (timeSeries.ValueKind != JsonValueKind.Object)
                {
                    return ErrorCodes.Extract.NoTimeSeries;
                }
                foreach (var day in timeSeries.EnumerateObject())
                {
                    if (!DateTime.TryParse(day.Name, out var timestamp))
                        continue;

                    var values = day.Value;

                    records[day.Name] = new StockRecord
                    {
                        Date = day.Name,
                        Open = values.GetProperty("1. open").GetString() ?? "",
                        High = values.GetProperty("2. high").GetString() ?? "",
                        Low = values.GetProperty("3. low").GetString() ?? "",
                        Close = values.GetProperty("4. close").GetString() ?? "",
                        Volume = values.GetProperty("5. volume").GetString() ?? ""
                    };
                }

                return records;
            }
            catch (Exception ex)
            {
                return Error.Unexpected("Extract.Exception", ex.Message);
            }
        }
    }
}