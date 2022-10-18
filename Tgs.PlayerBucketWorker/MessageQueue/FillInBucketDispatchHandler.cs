using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class FillInBucketDispatchHandler : BucketRequestDispatcher, IMessageHandler
{
	public FillInBucketDispatchHandler(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<FillInBucketDispatchHandler> logger)
		: base(messageSender, distributedResourceManager, logger)
	{
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = FillInRequest.Parser.ParseFrom(data.Span);

		var bidToken = await GetBidTokenAsync(
			request.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		var subject = $"tgs.bucket.worker.fill.{bidToken}";

		await MessageSender.SendAsync(
			subject,
			data,
			cancellationToken).ConfigureAwait(false);
	}
}
