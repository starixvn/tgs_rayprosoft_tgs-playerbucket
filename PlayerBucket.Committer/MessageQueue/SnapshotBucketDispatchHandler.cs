using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class SnapshotBucketDispatchHandler : BucketRequestDispatcher, IMessageHandler<ReadOnlyMemory<byte>>
{
	public SnapshotBucketDispatchHandler(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<SnapshotBucketDispatchHandler> logger) : base(messageSender, distributedResourceManager, logger)
	{
	}

	public async ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = SnapshotRequest.Parser.ParseFrom(data.Span);

		var bidToken = await GetBidTokenAsync(
			request.BucketId,
			cancellationToken).ConfigureAwait(false);

		var publishSubject = $"tgs.bucket.commit.snapshot.{bidToken}";

		await MessageSender.PublishAsync(
			publishSubject,
			data,
			cancellationToken).ConfigureAwait(false);
	}

}
