using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class CreateBucketDispatchProcessor : BucketRequestDispatcher, IMessageProcessor
{
	public CreateBucketDispatchProcessor(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<CreateBucketDispatchProcessor> logger)
		: base(messageSender, distributedResourceManager, logger)
	{
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = CreateBucketRequest.Parser.ParseFrom(data.Span);

		var bidToken = await GetBidTokenAsync(
			request.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		var subject = $"tgs.bucket.worker.create.{bidToken}";

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
