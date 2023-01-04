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

	public IAsyncEnumerable<BucketInfo> QueryPlayerBucketsAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var queryAfterDate = new DateTimeOffset(
			DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8D)).Date.AddDays(-30D),
			TimeSpan.FromHours(8D)).UtcDateTime;

		var notFullBuckets = _playerBucketDataAccess
			.GetPlayerNotFullBucketsAsync(playerCode, cancellationToken)
			.Select(row => new BucketInfo(
				row.Id,
				row.PlayerCode,
				row.BucketAmount,
				(BucketType)row.BucketType,
				row.CurrentBucketAmount,
				row.IsFull,
				row.CreatedOn,
				row.SnapshotId));
		var fullBuckets = _playerBucketDataAccess
			.GetPlayerFullBucketsAsync(playerCode, queryAfterDate, cancellationToken)
			.Select(row => new BucketInfo(
				row.Id,
				row.PlayerCode,
				row.BucketAmount,
				(BucketType)row.BucketType,
				row.CurrentBucketAmount,
				row.IsFull,
				row.CreatedOn,
				row.SnapshotId));

		return notFullBuckets.Concat(fullBuckets)
			.OrderByDescending(info => info.CreatedOn);
	}

	public IAsyncEnumerable<BucketEvent> QueryPlayerBucketHistoriesAsync(Guid bucketId, CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.GetBucketAllEventsAsync(bucketId, cancellationToken)
			.Take(1000)
			.Select(row => new BucketEvent(
				row.Id,
				row.BucketId,
				row.ChangeAmount,
				Enum.Parse<FillType>(row.FillType),
				row.ExternalId,
				JsonDocument.Parse(row.ChangeSets),
				row.CreatedOn));

	public IAsyncEnumerable<BucketEvent> QueryPlayerBucketHistoriesByIdAsync(
		Guid bucketId,
		Guid eventId,
		CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.GetBucketAllEventsAsync(bucketId, cancellationToken)
			.Where(row => row.Id.CompareTo(eventId) == 1)
			.Take(1000)
			.Select(row => new BucketEvent(
				row.Id,
				row.BucketId,
				row.ChangeAmount,
				Enum.Parse<FillType>(row.FillType),
				row.ExternalId,
				JsonDocument.Parse(row.ChangeSets),
				row.CreatedOn));

	public IAsyncEnumerable<BucketEvent> QueryPlayerBucketHistoriesByDateAsync(
		Guid bucketId,
		DateTime queryDate,
		CancellationToken cancellationToken = default)
	{
		var date = queryDate.ToUniversalTime();

		return _playerBucketDataAccess.GetBucketAllEventsAsync(bucketId, cancellationToken)
			.Where(row => row.CreatedOn > date)
			.Take(1000)
			.Select(row => new BucketEvent(
				row.Id,
				row.BucketId,
				row.ChangeAmount,
				Enum.Parse<FillType>(row.FillType),
				row.ExternalId,
				JsonDocument.Parse(row.ChangeSets),
				row.CreatedOn));
	}
}
