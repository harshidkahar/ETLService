using Azure.Messaging.ServiceBus;
using EtlService.Application.Configuration;
using EtlService.Application.Interfaces;
using Microsoft.Extensions.Options;


namespace EtlService.Infrastructure.Services;

public class ServiceBusMessagePublisher : IMessagePublisher
{
    private readonly ServiceBusClient _client;
    private readonly string _queueName;

    public ServiceBusMessagePublisher(IOptions<ServiceBusOptions> options)
    {
        var config = options.Value;
        _client = new ServiceBusClient(config.ConnectionString);
        _queueName = config.QueueName;
    }

    public async Task PublishAsync(string message, CancellationToken cancellationToken = default)
    {
        var sender = _client.CreateSender(_queueName);
        var serviceBusMessage = new ServiceBusMessage(message);
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}
