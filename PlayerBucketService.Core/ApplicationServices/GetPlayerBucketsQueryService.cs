using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class GetPlayerBucketsQueryService(ITgsRepository tgsRepository) : IQueryService<GetPlayerBucketsQuery, BucketInfo>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public IAsyncEnumerable<BucketInfo> QueryAsync(GetPlayerBucketsQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryPlayerBucketsAsync(query.PlayerCode, cancellationToken);
}
