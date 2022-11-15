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

	ValueTask<ReadBucketResult> ReadBucketsAsync(string playerCode, CancellationToken cancellationToken = default);

	ValueTask CleanBucketsAsync(
		string playerCode,
		Guid anchorBucketId,
		string? externalId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketInfo> GetBucketsByPlayerCodeAsync(string playerCode, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> GetBucketEventsAsync(Guid bucketId, CancellationToken cancellationToken = default);
}
