using System.Threading.Tasks.Dataflow;
using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBuckets.Committer;

internal class InProcessMessageQueue : IEventBus, IEventSource
{
	private readonly BroadcastBlock<BucketChangedEvent> _broadcastBucketChanged = new(null);
	private readonly BroadcastBlock<SnapshotEvent> _broadcastSnapshot = new(null);
	public IObservable<BucketChangedEvent> BucketChangedObservable => _broadcastBucketChanged.AsObservable();

	public IObservable<SnapshotEvent> SnapshotObservable => _broadcastSnapshot.AsObservable();

	public async ValueTask PublishBucketChangedAsync(BucketChangedEvent evt, CancellationToken cancellationToken = default)
		=> _ = await _broadcastBucketChanged.SendAsync(
			evt,
			cancellationToken).ConfigureAwait(false);

	public async ValueTask PublishSnapshotRequestAsync(SnapshotEvent evt, CancellationToken cancellationToken = default)
		=> _ = await _broadcastSnapshot.SendAsync(
			evt,
			cancellationToken).ConfigureAwait(false);
}
