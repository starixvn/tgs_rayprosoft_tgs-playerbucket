namespace Tgs.Service.PlayerBuckets;

public interface ITgsRepository
{
	IAsyncEnumerable<BucketInfo> QueryPlayerBucketsAsync(string playerCode, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> QueryPlayerBucketHistoriesAsync(Guid bucketId, CancellationToken cancellationToken = default);
}
