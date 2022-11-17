using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBuckets.Committer;

public interface ITgsRepository
{
	ValueTask InsertBucketEventsAsync(IEnumerable<BucketEvent> events, CancellationToken cancellationToken = default);

	ValueTask MigrateBucketEventsAsync(IEnumerable<BucketEvent> events, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, Guid snapshotId, CancellationToken cancellationToken = default);

	ValueTask<BucketInfo?> GetCreatedBucketSnapshotAsync(Guid bucketId, CancellationToken cancellationToken = default);

	ValueTask UpdatePlayerBucketAsync(BucketInfo bucketInfo, CancellationToken cancellationToken = default);
}
