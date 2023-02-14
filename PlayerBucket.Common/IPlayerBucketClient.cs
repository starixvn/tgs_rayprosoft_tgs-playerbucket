namespace Tgs.Service.PlayerBuckets;

public interface IPlayerBucketClient
{
	ValueTask<Guid> CreateBucketAsync(
		string playerCode,
		decimal amount,
		BucketType type,
		string? externalId,
		string? remark,
		CancellationToken cancellationToken = default);

	ValueTask FillInBucketAsync(
		string playerCode,
		decimal amount,
		FillType type,
		string? externalId,
		string? remark,
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
		string? remark,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketInfo> GetBucketsByPlayerCodeAsync(string playerCode, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> GetBucketEventsAsync(Guid bucketId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> GetBucketEventsAsync(Guid bucketId, Guid eventId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> GetBucketEventsAsync(Guid bucketId, DateTime queryDate, CancellationToken cancellationToken = default);

	ValueTask RequestCleanBucketsAsync(string playerCode, CleanBucketOrder order, CancellationToken cancellationToken = default);

	ValueTask<CleanBucketForm?> GetCleanBucketFormAsync(Guid formId, CancellationToken cancellationToken = default);

	ValueTask CompleteCleanBucketsAsync<TGameBalanceReturn>(
		Guid formId,
		ReviewCleanBucketRequest<TGameBalanceReturn> request,
		CancellationToken cancellationToken = default);

	ValueTask RejectCleanBucketsAsync<TGameBalanceReturn>(
		Guid formId,
		ReviewCleanBucketRequest<TGameBalanceReturn> request,
		CancellationToken cancellationToken = default);

	ValueTask PublishCleanBucketsFormCreatedEventAsync(Guid formId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<CleanBucketForm> QueryCleanBucketFomsAsync(
		string playerCode,
		CleanFlowStatus? flowStatus = null,
		CancellationToken cancellationToken = default);
}
