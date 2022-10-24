using Tgs.PlayerBuckets.Committer;

namespace Tgs.Service.PlayerBuckets;

public interface IEventSource
{
	ValueTask PublishBucketChangedAsync(BucketChangedEvent evt, CancellationToken cancellationToken = default);

	ValueTask PublishSnapshotRequestAsync(SnapshotEvent evt, CancellationToken cancellationToken = default);
}
