using EtlService.Domain.Entities;

namespace EtlService.Application.Interfaces
{
    public interface ICsvExporter
    {
        string SaveToCsv(IEnumerable<StockRecord> records, string symbol, DateTime date);
    }
}
