using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class GetBrandBucketTypesQueryService : IQueryService<GetBrandBucketTypesQuery, string>
{
	private readonly ITgsRepository _tgsRepository;

	public GetBrandBucketTypesQueryService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public IAsyncEnumerable<string> QueryAsync(
		GetBrandBucketTypesQuery query,
		CancellationToken cancellationToken = default)
		=> _tgsRepository.GetBrandBucketTypesAsync(query.BrandId, cancellationToken);
}
