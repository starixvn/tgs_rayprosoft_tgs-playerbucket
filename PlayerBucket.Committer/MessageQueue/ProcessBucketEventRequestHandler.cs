using MongoDB.Bson.Serialization;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class ProcessBucketEventRequestHandler : IMessageHandler
{
	private readonly IEventSource _eventSouce;

	public ProcessBucketEventRequestHandler(IEventSource eventSouce)
	{
		_eventSouce = eventSouce ?? throw new ArgumentNullException(nameof(eventSouce));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var eventData = BsonSerializer.Deserialize<BucketChangedEvent>(data.ToArray());

		return _eventSouce.PublishBucketChangedAsync(eventData, cancellationToken);
	}
}
