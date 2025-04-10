
namespace EtlService.Application.Configuration;

public class ServiceBusOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}
