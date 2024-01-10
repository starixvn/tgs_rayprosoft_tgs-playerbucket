using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class GetBrandBucketTypesQueryService(ITgsRepository tgsRepository) : IQueryService<GetBrandBucketTypesQuery, string>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public IAsyncEnumerable<string> QueryAsync(
		GetBrandBucketTypesQuery query,
		CancellationToken cancellationToken = default)
		=> _tgsRepository.GetBrandBucketTypesAsync(query.BrandId, cancellationToken);
}
