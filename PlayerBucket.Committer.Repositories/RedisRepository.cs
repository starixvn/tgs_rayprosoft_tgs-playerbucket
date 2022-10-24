using System.Text.Json;
using StackExchange.Redis;
using Tgs.PlayerBuckets.Committer;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucket.Committer.Repositories;

internal class RedisRepository : IBucketEventRepository, IDistributedResourceManager
{
	private readonly IDatabase _redisDatabase;

	public RedisRepository(IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
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

	public async ValueTask RemoveEventsAsync(Guid bucketId, int count, CancellationToken cancellationToken = default)
		=> _ = await _redisDatabase.ListLeftPopAsync(
			$"Tgs:Bucket:Events:{bucketId}",
			count,
			CommandFlags.FireAndForget).ConfigureAwait(false);

	public async ValueTask<string> BidAsync(Guid bucketId, string token, CancellationToken cancellationToken = default)
	{
		var redisKey = new RedisKey($"Tgs:BucketCommit:{bucketId}");

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

	public async ValueTask<string?> GetBidTokenAsync(Guid bucketId, CancellationToken cancellationToken = default)
	{
		var redisKey = new RedisKey($"Tgs:BucketCommit:{bucketId}");

		var token = await _redisDatabase
			.StringGetAsync(redisKey)
			.ConfigureAwait(false);

		return token;
	}

	public async ValueTask DiscardBidAsync(Guid bucketId, CancellationToken cancellationToken = default)
	{
		var redisKey = new RedisKey($"Tgs:BucketCommit:{bucketId}");

		_ = await _redisDatabase.KeyDeleteAsync(redisKey).ConfigureAwait(false);
	}
}
