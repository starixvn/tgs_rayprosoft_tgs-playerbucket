using System.Text.Json;
using Tgs.PlayerBuckets.DatabaseAccess;

namespace Tgs.Service.PlayerBuckets.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly PlayerBucketDataAccess _playerBucketDataAccess;

	public TgsRepository(PlayerBucketDataAccess playerBucketDataAccess)
	{
		_playerBucketDataAccess = playerBucketDataAccess ?? throw new ArgumentNullException(nameof(playerBucketDataAccess));
	}

	public IAsyncEnumerable<BucketInfo> GetPlayerNotFullBucketsAsync(string playerCode, CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.GetPlayerNotFullBucketsAsync(playerCode, cancellationToken)
			.Select(pb => new BucketInfo(
				pb.Id,
				pb.PlayerCode,
				pb.BucketAmount,
				(BucketType)pb.BucketType,
				pb.CurrentBucketAmount,
				pb.IsFull,
				DateTime.Now,
				pb.SnapshotId));

	public IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.GetBucketAllEventsAsync(bucketId, cancellationToken)
			.Select(evt => new BucketEvent(
				evt.Id,
				evt.BucketId,
				evt.ChangeAmount,
				Enum.Parse<FillType>(evt.FillType),
				evt.ExternalId,
				JsonDocument.Parse(evt.ChangeSets),
				evt.CreatedOn,
				evt.Remark));

	public IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, Guid snapshotId, CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.GetBucketAllEventsAsync(bucketId, snapshotId, cancellationToken)
			.Select(evt => new BucketEvent(
				evt.Id,
				evt.BucketId,
				evt.ChangeAmount,
				Enum.Parse<FillType>(evt.FillType),
				evt.ExternalId,
				JsonDocument.Parse(evt.ChangeSets),
				evt.CreatedOn,
				evt.Remark));

	public ValueTask<bool> GetHasMigrateAsync(string playerCode, CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.GetHasMigrateAsync(playerCode, cancellationToken);

	public async ValueTask SetHasMigrateAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		await _playerBucketDataAccess.SetPlayerHasMigrateAsync(
			playerCode,
			cancellationToken).ConfigureAwait(false);

		await _playerBucketDataAccess.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask UpdateLegacyBucketFullAsync(Guid bucketId, CancellationToken cancellationToken = default)
	{
		var legacyBucket = await _playerBucketDataAccess.FindPlayerBucketAsync(
			bucketId,
			cancellationToken).ConfigureAwait(false);

		if (legacyBucket is null)
			return;

		legacyBucket.IsFull = true;

		await _playerBucketDataAccess.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}
}
