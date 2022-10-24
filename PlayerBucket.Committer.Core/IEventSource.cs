using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBuckets.Committer;

public interface IEventSource
{
	ValueTask PublishBucketChangedAsync(BucketChangedEvent evt, CancellationToken cancellationToken = default);

	ValueTask PublishSnapshotRequestAsync(SnapshotEvent evt, CancellationToken cancellationToken = default);
}
