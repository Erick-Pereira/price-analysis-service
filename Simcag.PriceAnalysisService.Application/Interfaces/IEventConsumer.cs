using System.Threading;
using System.Threading.Tasks;

namespace Simcag.PriceAnalysisService.Application.Interfaces;

public interface IApplicationEventConsumer<TEvent>
{
    Task ConsumeAsync(TEvent @event, CancellationToken cancellationToken = default);
}