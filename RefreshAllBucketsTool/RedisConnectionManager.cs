using System.Text.Json;
using StackExchange.Redis;
using Tgs.Service.PlayerBuckets;

namespace RefreshAllBucketsTool;

internal class RedisConnectionManager : IDisposable
{
	private readonly ConnectionMultiplexer _connectionMultiplexer;
	private bool _disposedValue;

	public IDatabase Database { get; }

	public RedisConnectionManager(string redisConnectionString)
	{
		_connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);

		Database = _connectionMultiplexer.GetDatabase();
	}

	public IAsyncEnumerable<BucketInfo> GetAllCreateBucketsAsync()
		=> _connectionMultiplexer.GetServers()
			.ToAsyncEnumerable()
			.SelectMany(server => server.KeysAsync(pattern: "Tgs:Bucket:CreateBuckets:*", cursor: 0L, pageSize: 10000))
			.SelectMany(key => Database.HashGetAllAsync(key)
				.ToAsyncEnumerable())
			.SelectMany(arr => arr.ToAsyncEnumerable())
			.Select(entry => JsonSerializer.Deserialize<BucketInfo>((string)entry.Value!, PlayerBucketUtility.SerializerOptions))
			.Where(info => info is not null)
			.Select(info => info!);

	public IAsyncEnumerable<Guid> GetAllBucketIdsAsync()
		=> _connectionMultiplexer.GetServers()
			.ToAsyncEnumerable()
			.SelectMany(server => server.KeysAsync(pattern: "Tgs:Bucket:Events:*", cursor: 0L, pageSize: 10000))
			.Select(key => ((string)key)!.Split(":").Last())
			.Select(Guid.Parse);

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_connectionMultiplexer.Close();
				_connectionMultiplexer.Dispose();
			}

			_disposedValue = true;
		}
	}
}
