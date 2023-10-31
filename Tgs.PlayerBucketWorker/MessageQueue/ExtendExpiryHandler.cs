using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class ExtendExpiryHandler : IMessageHandler
{
	private readonly IBucketDispatcher _bucketDispatcher;

	public ExtendExpiryHandler(IBucketDispatcher bucketDispatcher)
	{
		_bucketDispatcher = bucketDispatcher ?? throw new ArgumentNullException(nameof(bucketDispatcher));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = ExtendExpiryRequest.Parser.ParseFrom(data.Span);

		_bucketDispatcher.ExtendExpiry(request.PlayerCode);

		return ValueTask.CompletedTask;
	}
}
