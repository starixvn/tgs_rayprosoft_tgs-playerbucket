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
}
