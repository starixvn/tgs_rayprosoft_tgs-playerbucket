using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class GetCleanPlayerBucketsFormQueryService(ITgsRepository tgsRepository)
		: IGetService<GetCleanPlayerBucketsFormQuery, CleanPlayerBucketForm?>
	, IGetService<GetCleanPlayerBucketsFormV2Query, CleanPlayerBucketForm?>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public ValueTask<CleanPlayerBucketForm?> GetAsync(GetCleanPlayerBucketsFormQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.GetCleanPlayerBucketFormV2Async(query.FormId, cancellationToken);

	public ValueTask<CleanPlayerBucketForm?> GetAsync(GetCleanPlayerBucketsFormV2Query query, CancellationToken cancellationToken = default)
		=> _tgsRepository.GetCleanPlayerBucketFormV2Async(query.FormId, cancellationToken);
}
