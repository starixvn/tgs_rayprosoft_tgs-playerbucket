namespace Tgs.Service.PlayerBuckets;

public interface IBucketStore
{
	string PlayerCode { get; }

	ValueTask<Guid> CreateBucketAsync(
		decimal amount,
		BucketType type,
		string? externalId,
		string? remark,
		CancellationToken cancellationToken = default);

	ValueTask<BucketInfo?> GetNotFullBucketAsync(CancellationToken cancellationToken = default);

	ValueTask FillInBucketAsync(
		BucketInfo bucket,
		FillInBucketInfo fillInInfo,
		string? remark,
		CancellationToken cancellationToken = default);

	ValueTask<ReadBucketResult> GetBucketAmountAsync(CancellationToken cancellationToken = default);

	ValueTask CleanUpBucketsAsync(
		Guid anchorBucketId,
		string? externalId,
		string? remark,
		CancellationToken cancellationToken = default);
}
