namespace Tgs.Service.PlayerBuckets;

public interface ITgsRepository
{
	IAsyncEnumerable<BucketInfo> QueryPlayerBucketsAsync(string playerCode, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> QueryPlayerBucketHistoriesAsync(Guid bucketId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> QueryPlayerBucketHistoriesByIdAsync(Guid bucketId, Guid eventId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BucketEvent> QueryPlayerBucketHistoriesByDateAsync(Guid bucketId, DateTime queryDate, CancellationToken cancellationToken = default);

	ValueTask UpsertCleanPlayerBucketFormAsync(CleanPlayerBucketForm form, CleanPlayerBucketForm? previousForm, CancellationToken cancellationToken = default);

	ValueTask<CleanPlayerBucketForm?> GetCleanPlayerBucketFormAsync(Guid formId, CancellationToken cancellationToken = default);
	ValueTask<CleanPlayerBucketForm?> GetCleanPlayerBucketFormV2Async(Guid formId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<CleanPlayerBucketForm> QueryCleanPlayerBucketFormsAsync(
		string playerCode,
		CleanFlowStatus? flowStatus,
		CancellationToken cancellationToken = default);

	ValueTask SetBrandBucketTypesAsync(Guid brandId, IEnumerable<string> bucketTypes, CancellationToken cancellationToken = default);

	IAsyncEnumerable<string> GetBrandBucketTypesAsync(Guid brandId, CancellationToken cancellationToken = default);
}
