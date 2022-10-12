namespace Tgs.Service.PlayerBuckets;

public interface ITgsRepository
{
	ValueTask AddPlayerBucketAsync(Guid createId, Guid bucketId, BucketCreateInfo bucket, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> GetBucetAllEventsAsync(Guid bucketId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketInfo> GetPlayerNotFullBucketsAsync(string playerCode, CancellationToken cancellationToken = default);

	ValueTask FillInBucketAsync(
		Guid createId,
		Guid bucketId,
		FillInBucketInfo bucketInfo,
		CancellationToken cancellationToken = default);

	ValueTask UpdatePlayerBucketAsync(BucketInfo bucketInfo, decimal usedAmount, CancellationToken cancellationToken = default);
}
