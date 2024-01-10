using MongoDB.Bson.Serialization;
using Tgs.Service.PlayerBuckets;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class ProcessBucketEventRequestHandler(IEventSource eventSouce) : IMessageHandler<ReadOnlyMemory<byte>>
{
	private readonly IEventSource _eventSouce = eventSouce ?? throw new ArgumentNullException(nameof(eventSouce));

	public ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var eventData = BsonSerializer.Deserialize<BucketChangedEvent>(data.ToArray());

		return _eventSouce.PublishBucketChangedAsync(eventData, cancellationToken);
	}
}
