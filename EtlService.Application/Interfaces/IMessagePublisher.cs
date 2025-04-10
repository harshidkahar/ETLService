
namespace EtlService.Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(string message, CancellationToken cancellationToken = default);
}
