using Microsoft.EntityFrameworkCore;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;

namespace Tgs.PlayerBuckets.DatabaseAccess;

internal class PlayerBucketDataAccess
{
	private readonly TgsContext _dbContext;

	public PlayerBucketDataAccess(TgsContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public IAsyncEnumerable<PlayerBucket> GetPlayerNotFullBucketsAsync(string playerCode, CancellationToken cancellationToken = default)
		=> _dbContext.PlayerBuckets
			.Where(pb => pb.PlayerCode == playerCode
				&& pb.IsFull == false)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());

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
}
