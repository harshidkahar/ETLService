using ErrorOr;
using EtlService.Domain.Entities;

namespace EtlService.Application.Interfaces;

public interface IExtractService
{
    Task<ErrorOr<Dictionary<string, StockRecord>>> ExtractDailyStockData(string symbol, string interval);
}
