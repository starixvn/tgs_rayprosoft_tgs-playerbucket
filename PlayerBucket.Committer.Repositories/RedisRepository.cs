using System.Text.Json;
using StackExchange.Redis;
using Tgs.PlayerBuckets.Committer;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucket.Committer.Repositories;

internal class RedisRepository : IBucketEventRepository
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

	public async ValueTask RemoveEventsAsync(Guid bucketId, int length, CancellationToken cancellationToken = default)
		=> _ = await _redisDatabase.ListLeftPopAsync(
			$"Tgs:Bucket:Events:{bucketId}",
			length,
			CommandFlags.FireAndForget).ConfigureAwait(false);
}
