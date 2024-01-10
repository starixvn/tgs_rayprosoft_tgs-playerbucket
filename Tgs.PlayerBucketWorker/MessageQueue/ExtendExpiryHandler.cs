using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class ExtendExpiryHandler(IBucketDispatcher bucketDispatcher) : IMessageHandler<ReadOnlyMemory<byte>>
{
	private readonly IBucketDispatcher _bucketDispatcher = bucketDispatcher ?? throw new ArgumentNullException(nameof(bucketDispatcher));

	public ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = ExtendExpiryRequest.Parser.ParseFrom(data.Span);

		_bucketDispatcher.ExtendExpiry(request.PlayerCode);

		return ValueTask.CompletedTask;
	}
}
