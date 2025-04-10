

namespace EtlService.Application.Configuration;

public class AlphaVantageOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Interval { get; set; } = "15min";
}

