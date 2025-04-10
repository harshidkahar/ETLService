
namespace EtlService.Domain.Entities;

public class DownloadLogEntry
{
    public string Symbol { get; set; }
    public DateTime Date { get; set; }
    public string Interval { get; set; }
}
