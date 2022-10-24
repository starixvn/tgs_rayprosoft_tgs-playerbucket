using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.Service.PlayerBuckets.Repositories.Entities;

namespace Tgs.Service.PlayerBuckets.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly TgsContext _dbContext;

	public TgsRepository(TgsContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public IAsyncEnumerable<BucketInfo> GetPlayerNotFullBucketsAsync(string playerCode, CancellationToken cancellationToken = default)
		=> _dbContext.PlayerBuckets
			.Where(pb => pb.PlayerCode == playerCode
				&& pb.IsFull == false)
			.Select(pb => new BucketInfo(
				pb.Id,
				pb.PlayerCode,
				pb.BucketAmount,
				(BucketType)pb.BucketType,
				pb.CurrentBucketAmount,
				pb.IsFull,
				pb.SnapshotId))
			.ToArrayAsync(cancellationToken)
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());

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
}
