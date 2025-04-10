
namespace EtlService.Application.Models;

public class EtlRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string Interval { get; set; } = "15min";
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
}
