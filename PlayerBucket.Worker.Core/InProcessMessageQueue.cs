using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;

namespace Tgs.Service.PlayerBuckets;

internal class InProcessMessageQueue : IEventBus, IEventSource
{
	private readonly BroadcastBlock<BucketChangedEvent> _broadcastBlock = new(null);

	public IObservable<BucketChangedEvent> BucketChangedObservable => _broadcastBlock.AsObservable();

	public async ValueTask PublishBucketChangedAsync(BucketChangedEvent evt, CancellationToken cancellationToken = default)
	{
		_ = await _broadcastBlock.SendAsync(evt).ConfigureAwait(false);
	}
}
