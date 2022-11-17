using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;

namespace Tgs.PlayerBuckets.DatabaseAccess;

internal class PlayerBucketDataAccess
{
	private readonly TgsContext _dbContext;
	private readonly ILogger<PlayerBucketDataAccess> _logger;

	public PlayerBucketDataAccess(TgsContext dbContext, ILogger<PlayerBucketDataAccess> logger)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public IAsyncEnumerable<PlayerBucket> GetPlayerNotFullBucketsAsync(string playerCode, CancellationToken cancellationToken = default)
		=> _dbContext.PlayerBuckets.AsQueryable()
			.Where(pb => pb.PlayerCode == playerCode
				&& pb.IsFull == false)
			.ToAsyncEnumerable()
			.Do(_ => cancellationToken.ThrowIfCancellationRequested());

	public IAsyncEnumerable<PlayerBucket> GetPlayerFullBucketsAsync(
		string playerCode,
		DateTime queryAfterDate,
		CancellationToken cancellationToken = default)
		=> _dbContext.PlayerBuckets.AsQueryable()
			.Where(pb => pb.PlayerCode == playerCode
				&& pb.IsFull == true
				&& pb.CreatedOn >= queryAfterDate.ToUniversalTime())
			.ToAsyncEnumerable()
			.Do(_ => cancellationToken.ThrowIfCancellationRequested());

	public IAsyncEnumerable<PlayerBucketEventSource> GetBucketAllEventsAsync(Guid bucketId, CancellationToken cancellationToken = default)
		=> _dbContext.PlayerBucketEventSources
			.Where(evt => evt.BucketId == bucketId)
			.OrderBy(evt => evt.Id)
			.ToArrayAsync(cancellationToken)
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());

	public IAsyncEnumerable<PlayerBucketEventSource> GetBucketAllEventsAsync(Guid bucketId, Guid snapshotId, CancellationToken cancellationToken = default)
		=> _dbContext.PlayerBucketEventSources
			.FromSqlInterpolated($@"
				SELECT
					id,
					bucket_id,
					change_amount,
					change_sets,
					created_on,
					external_id,
					fill_type
				FROM
					dbo.player_bucket_event_source
				WHERE
					bucket_id = {bucketId}
					AND
					id > {snapshotId}
				ORDER BY
					id")
			.ToArrayAsync(cancellationToken)
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());

	public async ValueTask InsertBucketEventsAsync(IEnumerable<PlayerBucketEventSource> events, CancellationToken cancellationToken = default)
		=> await _dbContext.PlayerBucketEventSources
			.AddRangeAsync(events, cancellationToken)
			.ConfigureAwait(false);

	public async ValueTask MigrateBucketEventsAsync(IEnumerable<PlayerBucketEventSource> events, CancellationToken cancellationToken = default)
	{
		var preInsertIds = events.Select(evt => evt.Id).ToArray();

		var alreadyInsertRows = await _dbContext.PlayerBucketEventSources
			.Where(evt => preInsertIds.Contains(evt.Id))
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var newEvents = events
			.Where(evt => !alreadyInsertRows.Any(row => row.Id == evt.Id))
			.ToArray();

		_logger.LogInformation("{newEvents}", newEvents);

		if (newEvents.Length > 0)
			await InsertBucketEventsAsync(
				newEvents,
				cancellationToken).ConfigureAwait(false);
	}

	public ValueTask<PlayerBucket?> FindPlayerBucketAsync(Guid bucketId, CancellationToken cancellationToken = default)
		=> _dbContext.PlayerBuckets
			.FindAsync(new object?[] { bucketId }, cancellationToken: cancellationToken);

	public async ValueTask<PlayerBucket> GetOrAddPlayerBucketAsync(Guid bucketId, CancellationToken cancellationToken = default)
		=> (await FindPlayerBucketAsync(
			bucketId,
			cancellationToken: cancellationToken).ConfigureAwait(false))
			?? (await _dbContext.PlayerBuckets.AddAsync(
				new PlayerBucket
				{
					Id = bucketId,
					CreatedOn = DateTime.UtcNow
				},
				cancellationToken).ConfigureAwait(false)).Entity;

	public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
		=> await _dbContext.SaveChangesAsync(cancellationToken);

	public async ValueTask<bool> GetHasMigrateAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var record = await _dbContext.PlayerBucketMigrations.FindAsync(
			new object?[] { playerCode },
			cancellationToken: cancellationToken).ConfigureAwait(false);

		return record is not null;
	}

	public async ValueTask SetPlayerHasMigrateAsync(string playerCode, CancellationToken cancellationToken = default)
		=> _ = await _dbContext.PlayerBucketMigrations.AddAsync(
			new PlayerBucketMigration
			{
				PlayerCode = playerCode
			},
			cancellationToken).ConfigureAwait(false);
}
