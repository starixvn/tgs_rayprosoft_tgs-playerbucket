using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class GetPlayerBucketHistoriesQueryService
	: IQueryService<GetPlayerBucketHistoriesQuery, BucketEvent>,
	IQueryService<GetPlayerBucketHistoriesByIdQuery, BucketEvent>,
	IQueryService<GetPlayerBucketHistoriesByDateQuery, BucketEvent>
{
	private readonly ITgsRepository _tgsRepository;

	public GetPlayerBucketHistoriesQueryService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public IAsyncEnumerable<BucketEvent> QueryAsync(GetPlayerBucketHistoriesQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryPlayerBucketHistoriesAsync(query.BucketId, cancellationToken);

	public IAsyncEnumerable<BucketEvent> QueryAsync(GetPlayerBucketHistoriesByIdQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryPlayerBucketHistoriesByIdAsync(query.BucketId, query.EventId, cancellationToken);

	public IAsyncEnumerable<BucketEvent> QueryAsync(GetPlayerBucketHistoriesByDateQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryPlayerBucketHistoriesByDateAsync(query.BucketId, query.QueryDate, cancellationToken);
}
