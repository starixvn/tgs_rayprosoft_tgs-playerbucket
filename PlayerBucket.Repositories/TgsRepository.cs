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

	public async ValueTask AddPlayerBucketAsync(Guid createId, Guid bucketId, BucketCreateInfo bucket, CancellationToken cancellationToken = default)
	{
		var playerBucketEvent = new PlayerBucketEventSource
		{
			Id = createId,
			BucketId = bucketId,
			ChangeAmount = bucket.Amount,
			ExternalId = null,
			FillType = FillType.Create.ToString(),
			CreatedOn = DateTime.UtcNow,
			ChangeSets = bucket.CreateJsonPatch(bucketId)
		};

		_ = await _dbContext.PlayerBucketEventSources
			.AddAsync(playerBucketEvent, cancellationToken)
			.ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

	public async ValueTask FillInBucketAsync(
		Guid createId,
		Guid bucketId,
		FillInBucketInfo bucketInfo,
		CancellationToken cancellationToken = default)
	{
		var playerBucketEvent = new PlayerBucketEventSource
		{
			Id = createId,
			BucketId = bucketId,
			ChangeAmount = -bucketInfo.Amount,
			ExternalId = bucketInfo.ExternalId,
			FillType = bucketInfo.Type.ToString(),
			CreatedOn = DateTime.UtcNow,
			ChangeSets = bucketInfo.CreateJsonPatch()
		};

		_ = await _dbContext.PlayerBucketEventSources
			.AddAsync(playerBucketEvent, cancellationToken)
			.ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public IAsyncEnumerable<BucketEvent> GetBucetAllEventsAsync(Guid bucketId, CancellationToken cancellationToken = default)
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

	public async ValueTask UpdatePlayerBucketAsync(BucketInfo bucketInfo, decimal usedAmount, CancellationToken cancellationToken = default)
	{
		var playerBucket = (await _dbContext.PlayerBuckets
			.FindAsync(new object?[] { bucketInfo.Id }, cancellationToken: cancellationToken)
			.ConfigureAwait(false))
			?? (await _dbContext.PlayerBuckets.AddAsync(
				new PlayerBucket
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
		playerBucket.CurrentBucketAmount = usedAmount;

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}
}
