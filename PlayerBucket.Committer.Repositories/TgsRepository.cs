using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.PlayerBuckets.Committer.Repositories.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBuckets.Committer.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly TgsContext _dbContext;

	public TgsRepository(TgsContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public async ValueTask InsertBucketEventsAsync(IEnumerable<BucketEvent> events, CancellationToken cancellationToken = default)
	{
		await _dbContext.PlayerBucketEventSources
			.AddRangeAsync(events.Select(evt => new PlayerBucketEventSource
			{
				Id = evt.Id,
				BucketId = evt.BucketId,
				ChangeAmount = evt.ChangeAmount,
				FillType = evt.Type.ToString(),
				ExternalId = evt.ExternalId,
				ChangeSets = JsonSerializer.Serialize(evt.ChangeSets),
				CreatedOn = evt.CreatedOn.ToUniversalTime()
			}), cancellationToken)
			.ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, CancellationToken cancellationToken = default)
		=> _dbContext.PlayerBucketEventSources
			.Where(evt => evt.BucketId == bucketId)
			.OrderBy(evt => evt.Id)
			.Select(evt => new
			{
				evt.Id,
				evt.BucketId,
				evt.ChangeAmount,
				evt.FillType,
				evt.ExternalId,
				evt.ChangeSets,
				evt.CreatedOn
			})
			.ToArrayAsync(cancellationToken)
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable())
			.Select(evt => new BucketEvent(
				evt.Id,
				evt.BucketId,
				evt.ChangeAmount,
				Enum.Parse<FillType>(evt.FillType),
				evt.ExternalId,
				JsonDocument.Parse(evt.ChangeSets),
				evt.CreatedOn));

	public IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, Guid snapshotId, CancellationToken cancellationToken = default)
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
			.SelectMany(arr => arr.ToAsyncEnumerable())
			.Select(evt => new BucketEvent(
				evt.Id,
				evt.BucketId,
				evt.ChangeAmount,
				Enum.Parse<FillType>(evt.FillType),
				evt.ExternalId,
				JsonDocument.Parse(evt.ChangeSets),
				evt.CreatedOn));

	public async ValueTask<BucketInfo?> GetCreatedBucketSnapshotAsync(Guid bucketId, CancellationToken cancellationToken = default)
	{
		var record = await _dbContext.PlayerBuckets
			.FindAsync(new object?[] { bucketId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return record is null
			? null
			: new BucketInfo(
				record.Id,
				record.PlayerCode,
				record.BucketAmount,
				(BucketType)record.BucketType,
				record.CurrentBucketAmount,
				record.IsFull,
				record.SnapshotId);
	}

	public async ValueTask UpdatePlayerBucketAsync(BucketInfo bucketInfo, CancellationToken cancellationToken = default)
	{
		var playerBucket = (await _dbContext.PlayerBuckets
			.FindAsync(new object?[] { bucketInfo.Id }, cancellationToken: cancellationToken)
			.ConfigureAwait(false))
			?? (await _dbContext.PlayerBuckets.AddAsync(
				new Tgs.PlayerBuckets.Committer.Repositories.Entities.PlayerBucket
				{
					Id = bucketInfo.Id,
					PlayerCode = bucketInfo.PlayerCode,
					BucketType = (short)bucketInfo.Type,
					BucketAmount = bucketInfo.Amount,
					CreatedOn = DateTime.UtcNow
				},
				cancellationToken).ConfigureAwait(false)).Entity;

		playerBucket.IsFull = bucketInfo.IsFull;
		playerBucket.SnapshotId = bucketInfo.SnapshotId;
		playerBucket.CurrentBucketAmount = bucketInfo.FillInAmount;

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}
}
