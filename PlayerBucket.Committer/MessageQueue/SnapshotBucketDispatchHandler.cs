using Tgs.MessageQueue;
using Tgs.PlayerBucketWorker.MessageQueue;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class SnapshotBucketDispatchHandler : BucketRequestDispatcher, IMessageHandler
{
	public SnapshotBucketDispatchHandler(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<SnapshotBucketDispatchHandler> logger) : base(messageSender, distributedResourceManager, logger)
	{
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = SnapshotRequest.Parser.ParseFrom(data.Span);

		var bidToken = await GetBidTokenAsync(
			request.BucketId,
			cancellationToken).ConfigureAwait(false);

		var subject = $"tgs.bucket.commit.snapshot.{bidToken}";

		await MessageSender.PublishAsync(
			subject,
			data,
			cancellationToken).ConfigureAwait(false);
	}
}
