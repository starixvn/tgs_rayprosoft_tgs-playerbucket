using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBuckets.Committer;

public interface IBucketEventRepository
{
	IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, CancellationToken cancellationToken = default);

	ValueTask RemoveEventsAsync(Guid bucketId, int length, CancellationToken cancellationToken = default);
}
