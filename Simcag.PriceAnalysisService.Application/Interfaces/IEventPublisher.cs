using System.Threading;
using System.Threading.Tasks;

namespace Simcag.PriceAnalysisService.Application.Interfaces;

public interface IApplicationEventPublisher<TEvent>
{
    Task PublishAsync(TEvent @event, CancellationToken cancellationToken = default);
}