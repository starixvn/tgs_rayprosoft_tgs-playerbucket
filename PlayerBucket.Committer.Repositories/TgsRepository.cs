using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tgs.PlayerBuckets.DatabaseAccess;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBuckets.Committer.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly PlayerBucketDataAccess _playerBucketDataAccess;
	private readonly ILogger<TgsRepository> _logger;

	public TgsRepository(PlayerBucketDataAccess playerBucketDataAccess, ILogger<TgsRepository> logger)
	{
		_playerBucketDataAccess = playerBucketDataAccess ?? throw new ArgumentNullException(nameof(playerBucketDataAccess));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask InsertBucketEventsAsync(IEnumerable<BucketEvent> events, CancellationToken cancellationToken = default)
	{
		await _playerBucketDataAccess.InsertBucketEventsAsync(
			events.Select(evt => new PlayerBucketEventSource
			{
				Id = evt.Id,
				BucketId = evt.BucketId,
				ChangeAmount = evt.ChangeAmount,
				FillType = evt.Type.ToString(),
				ExternalId = evt.ExternalId,
				ChangeSets = JsonSerializer.Serialize(evt.ChangeSets),
				CreatedOn = evt.CreatedOn.ToUniversalTime()
			}),
			cancellationToken).ConfigureAwait(false);

		await _playerBucketDataAccess.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.GetBucketAllEventsAsync(bucketId, cancellationToken)
			.Select(evt => new BucketEvent(
				evt.Id,
				evt.BucketId,
				evt.ChangeAmount,
				Enum.Parse<FillType>(evt.FillType),
				evt.ExternalId,
				JsonDocument.Parse(evt.ChangeSets),
				evt.CreatedOn));

	public IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, Guid snapshotId, CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.GetBucketAllEventsAsync(bucketId, snapshotId, cancellationToken)
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
		var record = await _playerBucketDataAccess.FindPlayerBucketAsync(
			bucketId,
			cancellationToken).ConfigureAwait(false);

		return record is null
			? null
			: new BucketInfo(
				record.Id,
				record.PlayerCode,
				record.BucketAmount,
				(BucketType)record.BucketType,
				record.CurrentBucketAmount,
				record.IsFull,
				DateTime.Now,
				record.SnapshotId);
	}

	public async ValueTask UpdatePlayerBucketAsync(BucketInfo bucketInfo, CancellationToken cancellationToken = default)
	{
		var playerBucket = await _playerBucketDataAccess.GetOrAddPlayerBucketAsync(bucketInfo.Id, cancellationToken);

		playerBucket.PlayerCode = bucketInfo.PlayerCode;
		playerBucket.BucketType = (short)bucketInfo.Type;
		playerBucket.BucketAmount = bucketInfo.Amount;
		playerBucket.IsFull = bucketInfo.IsFull;
		playerBucket.SnapshotId = bucketInfo.SnapshotId;
		playerBucket.CurrentBucketAmount = bucketInfo.FillInAmount;

		await _playerBucketDataAccess.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask MigrateBucketEventsAsync(IEnumerable<BucketEvent> events, CancellationToken cancellationToken = default)
	{
		_playerBucketDataAccess.RejectChanges();

		var alreadyInsertIds = await _playerBucketDataAccess.GetBucketEventsAsync(
			events.Select(evt => evt.Id),
			cancellationToken)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		_logger.LogWarning("{alreadyInsertIds}", alreadyInsertIds);

		var newEvents = events.Where(evt => !alreadyInsertIds.Contains(evt.Id)).ToArray();

		_logger.LogWarning("{@newEvents}", newEvents);

		if (newEvents is not null && newEvents.Length > 0)
		{
			await _playerBucketDataAccess.InsertBucketEventsAsync(
				newEvents.Select(evt => new PlayerBucketEventSource
				{
					Id = evt.Id,
					BucketId = evt.BucketId,
					ChangeAmount = evt.ChangeAmount,
					FillType = evt.Type.ToString(),
					ExternalId = evt.ExternalId,
					ChangeSets = JsonSerializer.Serialize(evt.ChangeSets),
					CreatedOn = evt.CreatedOn.ToUniversalTime()
				}),
				cancellationToken).ConfigureAwait(false);

			await _playerBucketDataAccess.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}
