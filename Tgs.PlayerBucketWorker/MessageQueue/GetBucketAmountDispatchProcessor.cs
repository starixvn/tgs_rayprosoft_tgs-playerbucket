using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class GetBucketAmountDispatchProcessor : BucketRequestDispatcher, IMessageProcessor
{
	public GetBucketAmountDispatchProcessor(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<GetBucketAmountDispatchProcessor> logger) : base(messageSender, distributedResourceManager, logger)
	{
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = GetBucketAmountRequest.Parser.ParseFrom(data.Span);

		var bidToken = await GetBidTokenAsync(
			request.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		var subject = $"tgs.bucket.worker.amount.{bidToken}";

		var response = await MessageSender.RequestAsync(
			subject,
			data,
			cancellationToken).ConfigureAwait(false);

		await ExtendBidAsync(
			request.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		return response;
	}
}
