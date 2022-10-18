using StackExchange.Redis;

namespace Tgs.Service.PlayerBuckets.Repositories;

internal class RedisDistributedResourceManager : IDistributedResourceManager
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
			expiry: TimeSpan.FromMinutes(10),
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
}
