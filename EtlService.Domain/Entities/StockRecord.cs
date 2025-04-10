namespace EtlService.Domain.Entities;

public class StockRecord
{
    public string Date { get; set; } = string.Empty;
    public string Open { get; set; } = string.Empty;
    public string High { get; set; } = string.Empty;
    public string Low { get; set; } = string.Empty;
    public string Close { get; set; } = string.Empty;
    public string Volume { get; set; } = string.Empty;
}
