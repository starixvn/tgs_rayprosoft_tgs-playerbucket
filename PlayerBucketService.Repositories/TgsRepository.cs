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
}
