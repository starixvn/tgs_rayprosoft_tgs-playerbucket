using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class GetPlayerBucketHistoriesQueryService : IQueryService<GetPlayerBucketHistoriesQuery, BucketEvent>
{
	private readonly ITgsRepository _tgsRepository;

	public GetPlayerBucketHistoriesQueryService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public IAsyncEnumerable<BucketEvent> QueryAsync(GetPlayerBucketHistoriesQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryPlayerBucketHistoriesAsync(query.BucketId, cancellationToken);
}
