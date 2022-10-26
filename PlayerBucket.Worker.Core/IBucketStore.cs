namespace Tgs.Service.PlayerBuckets;

public interface IBucketStore
{
	string PlayerCode { get; }

	ValueTask<Guid> CreateBucketAsync(decimal amount, BucketType type, string? externalId, CancellationToken cancellationToken = default);

	ValueTask<BucketInfo?> GetNotFullBucketAsync(CancellationToken cancellationToken = default);

	ValueTask FillInBucketAsync(BucketInfo bucket, FillInBucketInfo fillInInfo, CancellationToken cancellationToken = default);

	ValueTask<ReadBucketResult> GetBucketAmountAsync(CancellationToken cancellationToken = default);

	ValueTask CleanUpBucketsAsync(Guid eventId, CancellationToken cancellationToken = default);
}
