using MongoDB.Bson.Serialization;
using Tgs.MessageQueue;
using Tgs.PlayerBucketWorker.MessageQueue;
using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class ProcessBucketEventRequestDispatchHandler : BucketRequestDispatcher, IMessageHandler
{
	public ProcessBucketEventRequestDispatchHandler(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<ProcessBucketEventRequestHandler> logger)
		: base(messageSender, distributedResourceManager, logger)
	{
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = BsonSerializer.Deserialize<BucketChangedEvent>(data.ToArray());

		var bidToken = await GetBidTokenAsync(
			request.BucketId,
			cancellationToken).ConfigureAwait(false);

		var subject = $"tgs.bucket.commit.batch.{bidToken}";

		await MessageSender.PublishAsync(
			subject,
			data,
			cancellationToken).ConfigureAwait(false);
	}
}
