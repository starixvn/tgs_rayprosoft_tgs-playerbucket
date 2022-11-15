using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class GetPlayerBucketsQueryService : IQueryService<GetPlayerBucketsQuery, BucketInfo>
{
	private readonly ITgsRepository _tgsRepository;

	public GetPlayerBucketsQueryService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public IAsyncEnumerable<BucketInfo> QueryAsync(GetPlayerBucketsQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryPlayerBucketsAsync(query.PlayerCode, cancellationToken);
}
