namespace Tgs.Service.PlayerBuckets;

public interface ITgsRepository
{
	IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, Guid snapshotId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketInfo> GetPlayerNotFullBucketsAsync(string playerCode, CancellationToken cancellationToken = default);
}
