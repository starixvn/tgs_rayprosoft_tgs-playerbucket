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
			.FromSqlInterpolated($$"""
				SELECT
					id,
					bucket_id,
					change_amount,
					change_sets,
					created_on,
					external_id,
					fill_type,
					remark
				FROM
					dbo.player_bucket_event_source
				WHERE
					bucket_id = {{bucketId}}
					AND
					id > {{snapshotId}}
				ORDER BY
					id
			""")
			.ToArrayAsync(cancellationToken)
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());

	public async ValueTask InsertBucketEventsAsync(IEnumerable<PlayerBucketEventSource> events, CancellationToken cancellationToken = default)
		=> await _dbContext.PlayerBucketEventSources
			.AddRangeAsync(events, cancellationToken)
			.ConfigureAwait(false);

	public IAsyncEnumerable<Guid> GetBucketEventsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default)
		=> _dbContext.PlayerBucketEventSources.AsQueryable()
			.Where(evt => eventIds.Contains(evt.Id))
			.ToAsyncEnumerable()
			.Do(evt => cancellationToken.ThrowIfCancellationRequested())
			.Select(evt => evt.Id);

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

	public void RejectChanges()
	{
		foreach (var entry in _dbContext.ChangeTracker.Entries())
		{
			switch (entry.State)
			{
				case EntityState.Modified:
				case EntityState.Deleted:
					entry.State = EntityState.Modified; //Revert changes made to deleted entity.
					entry.State = EntityState.Unchanged;
					break;

				case EntityState.Added:
					entry.State = EntityState.Detached;
					break;
			}
		}
	}

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

	public async ValueTask UpsertPlayerBucketCleanFormAsync(PlayerBucketCleanForm formRow, CancellationToken cancellationToken = default)
	{
		var existsRow = await _dbContext.PlayerBucketCleanForms.FindAsync(
			new object?[] { formRow.Id },
			cancellationToken: cancellationToken).ConfigureAwait(false);

		if (existsRow is not null)
		{
			existsRow.CleaningBalance = formRow.CleaningBalance;
			existsRow.CleaningBucketAmount = formRow.CleaningBucketAmount;
			existsRow.GameBalanceReturn = formRow.GameBalanceReturn;
			existsRow.Status = formRow.Status;
		}
		else
			_ = await _dbContext.PlayerBucketCleanForms.AddAsync(
				formRow,
				cancellationToken).ConfigureAwait(false);
	}

	public ValueTask<PlayerBucketCleanForm?> FindPlayerBucketCleanFormAsync(Guid formId, CancellationToken cancellationToken = default)
		=> _dbContext.PlayerBucketCleanForms.FindAsync(
			new object?[] { formId },
			cancellationToken: cancellationToken);

	public async ValueTask InsertPlayerBucketCleanEventSourceAsync(
		PlayerBucketCleanEventSource eventSource,
		CancellationToken cancellationToken = default)
		=> await _dbContext.PlayerBucketCleanEventSources.AddAsync(
			eventSource,
			cancellationToken).ConfigureAwait(false);
}
