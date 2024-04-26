using MongoDB.Bson.Serialization;
using Tgs.Service.PlayerBuckets;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class ProcessBucketEventRequestHandler(
	IEventSource eventSouce,
	ILogger<ProcessBucketEventRequestHandler> logger)
	: IMessageHandler<ReadOnlyMemory<byte>>
{
	public ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var eventData = BsonSerializer.Deserialize<BucketChangedEvent>(data.ToArray());

		logger.LogInformation("Receive {BucketId}, {PlayerCode}", eventData.BucketId, eventData.PlayerCode);
		return eventSouce.PublishBucketChangedAsync(eventData, cancellationToken);
	}
}
