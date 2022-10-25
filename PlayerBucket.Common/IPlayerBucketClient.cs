namespace Tgs.Service.PlayerBuckets;

public interface IPlayerBucketClient
{
	ValueTask<Guid> CreateBucketAsync(
		string playerCode,
		decimal amount,
		BucketType type,
		string? externalId,
		CancellationToken cancellationToken = default);

	ValueTask FillInBucketAsync(
		string playerCode,
		decimal amount,
		FillType type,
		string? externalId,
		CancellationToken cancellationToken = default);

	ValueTask SnapshotBucketAsync(
		string playerCode,
		Guid bucketId,
		CancellationToken cancellationToken = default);

	ValueTask<decimal> GetBucketAmountAsync(string playerCode, CancellationToken cancellationToken = default);
}
