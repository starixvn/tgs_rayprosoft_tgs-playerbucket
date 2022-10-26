using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class CleanBucketDispatchHandler : BucketRequestDispatcher, IMessageHandler
{
	public CleanBucketDispatchHandler(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<CleanBucketDispatchHandler> logger) : base(messageSender, distributedResourceManager, logger)
	{
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = CleanBucketRequest.Parser.ParseFrom(data.Span);

		var bidToken = await GetBidTokenAsync(
			request.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		var subject = $"tgs.bucket.worker.clean.{bidToken}";

		await MessageSender.SendAsync(
			subject,
			data,
			cancellationToken).ConfigureAwait(false);

		await ExtendBidAsync(
			request.PlayerCode,
			bidToken,
			cancellationToken).ConfigureAwait(false);
	}
}
