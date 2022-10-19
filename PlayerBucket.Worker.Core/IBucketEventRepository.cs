namespace Tgs.Service.PlayerBuckets;

public interface IBucketEventRepository
{
	ValueTask AddPlayerBucketAsync(Guid createId, Guid bucketId, BucketCreateInfo bucket, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, Guid snapshotId, CancellationToken cancellationToken = default);

	ValueTask FillInBucketAsync(
		Guid createId,
		Guid bucketId,
		FillInBucketInfo bucketInfo,
		CancellationToken cancellationToken = default);
}
