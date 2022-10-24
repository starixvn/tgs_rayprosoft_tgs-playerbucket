using System.Text.Json;
using StackExchange.Redis;

namespace Tgs.Service.PlayerBuckets.Repositories;

internal class RedisDistributedResourceManager : IDistributedResourceManager, IBucketEventRepository
{
	private readonly IDatabase _redisDatabase;

	public RedisDistributedResourceManager(IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
	}

	public async ValueTask<string> BidAsync(string playerCode, string token, CancellationToken cancellationToken = default)
	{
		var redisKey = new RedisKey($"Tgs:Bucket:{playerCode}");

		var isGot = await _redisDatabase.StringSetAsync(
			redisKey,
			token,
			expiry: TimeSpan.FromMinutes(10D),
			when: When.NotExists).ConfigureAwait(false);

		return isGot
			? token
			: (string?)await _redisDatabase
				.StringGetAsync(redisKey)
				.ConfigureAwait(false) ?? string.Empty;
	}

	public async ValueTask<string?> GetBidTokenAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var redisKey = new RedisKey($"Tgs:Bucket:{playerCode}");

		var token = await _redisDatabase
			.StringGetAsync(redisKey)
			.ConfigureAwait(false);

		return token;
	}

	public async ValueTask DiscardBidAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var redisKey = new RedisKey($"Tgs:Bucket:{playerCode}");

		_ = await _redisDatabase.KeyDeleteAsync(redisKey).ConfigureAwait(false);
	}

	public async ValueTask AddPlayerBucketAsync(Guid createId, Guid bucketId, BucketCreateInfo bucket, CancellationToken cancellationToken = default)
	{
		var currentDate = DateTimeOffset.UtcNow;

		var record = new BucketEvent(
			createId,
			bucketId,
			bucket.Amount,
			FillType.Create,
			bucket.ExternalId,
			bucket.CreateJsonPatchDocument(bucketId),
			currentDate.UtcDateTime);

		var bucketSnapshot = new BucketInfo(
			bucketId,
			bucket.PlayerCode,
			bucket.Amount,
			bucket.Type,
			0M,
			false,
			createId);

		_ = await _redisDatabase.ListRightPushAsync(
			$"Tgs:Bucket:Events:{bucketId}",
			JsonSerializer.Serialize(record, PlayerBucketUtility.SerializerOptions))
			.ConfigureAwait(false);

		_ = await _redisDatabase.HashSetAsync(
			$"Tgs.Bucket:CreateBuckets:{bucket.PlayerCode}",
			bucketId.ToString(),
			JsonSerializer.Serialize(bucketSnapshot, PlayerBucketUtility.SerializerOptions))
			.ConfigureAwait(false);
	}

	public IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, Guid snapshotId, CancellationToken cancellationToken = default)
		=> GetBucketAllEventsAsync(bucketId, cancellationToken)
			.Where(evt => evt.Id.CompareTo(snapshotId) > 0)
			.ToArrayAsync(cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());

	public async ValueTask FillInBucketAsync(Guid createId, Guid bucketId, FillInBucketInfo bucketInfo, CancellationToken cancellationToken = default)
	{
		var currentDate = DateTimeOffset.UtcNow;

		var record = new BucketEvent(
			createId,
			bucketId,
			-bucketInfo.Amount,
			bucketInfo.Type,
			bucketInfo.ExternalId,
			bucketInfo.CreateJsonPatchDocument(),
			currentDate.UtcDateTime);

		var json = JsonSerializer.Serialize(record, PlayerBucketUtility.SerializerOptions);

		_ = await _redisDatabase.ListRightPushAsync(
			$"Tgs:Bucket:Events:{bucketId}",
			json).ConfigureAwait(false);
	}

	public IAsyncEnumerable<BucketEvent> GetBucketAllEventsAsync(Guid bucketId, CancellationToken cancellationToken = default)
		=> _redisDatabase
			.ListRangeAsync($"Tgs:Bucket:Events:{bucketId}")
			.ToAsyncEnumerable()
			.SelectMany(values => values.ToAsyncEnumerable())
			.Select(value => JsonSerializer.Deserialize<BucketEvent>((string)value!, PlayerBucketUtility.SerializerOptions))
			.Where(evt => evt is not null)
			.Select(evt => evt!)
			.ToArrayAsync(cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());

	public IAsyncEnumerable<BucketInfo> GetPlayerCreatedBucketsAsync(string playerCode, CancellationToken cancellationToken = default)
		=> _redisDatabase.HashGetAllAsync($"Tgs.Bucket:CreateBuckets:{playerCode}")
			.ToAsyncEnumerable()
			.SelectMany(entries => entries.ToAsyncEnumerable())
			.Select(entry => JsonSerializer.Deserialize<BucketInfo>((string)entry.Value!, PlayerBucketUtility.SerializerOptions))
			.Where(info => info is not null)
			.Select(info => info!)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());
}
