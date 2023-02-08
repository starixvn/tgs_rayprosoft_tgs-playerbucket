using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class GetCleanPlayerBucketsFormQueryService : IGetService<GetCleanPlayerBucketsFormQuery, CleanPlayerBucketForm?>
{
	private readonly ITgsRepository _tgsRepository;

	public GetCleanPlayerBucketsFormQueryService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public ValueTask<CleanPlayerBucketForm?> GetAsync(GetCleanPlayerBucketsFormQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.GetCleanPlayerBucketFormAsync(query.FormId, cancellationToken);
}
