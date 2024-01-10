using MongoDB.Bson.Serialization;
using Tgs.Service.PlayerBuckets;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class ProcessBucketEventRequestDispatchHandler(
	IMessageSender messageSender,
	IDistributedResourceManager distributedResourceManager,
	ILogger<ProcessBucketEventRequestHandler> logger)
	: BucketRequestDispatcher(
		messageSender,
		distributedResourceManager,
		logger), IMessageHandler<byte[]>
{
	public async ValueTask HandleAsync(
		string subject,
		byte[] data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = BsonSerializer.Deserialize<BucketChangedEvent>(data);

		var bidToken = await GetBidTokenAsync(
			request.BucketId,
			cancellationToken).ConfigureAwait(false);

		var publishSubject = $"tgs.bucket.commit.batch.{bidToken}";

		await MessageSender.PublishAsync(
			publishSubject,
			data,
			cancellationToken).ConfigureAwait(false);
	}
}
