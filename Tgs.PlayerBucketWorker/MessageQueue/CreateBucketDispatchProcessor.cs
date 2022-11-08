using NATS.Client;
using Polly;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class CreateBucketDispatchProcessor : BucketRequestDispatcher, IMessageProcessor
{
	private readonly ILogger<CreateBucketDispatchProcessor> _logger;

	public CreateBucketDispatchProcessor(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<CreateBucketDispatchProcessor> logger)
		: base(messageSender, distributedResourceManager, logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = CreateBucketRequest.Parser.ParseFrom(data.Span);

		return await Policy
		   .Handle<NATSNoRespondersException>()
		   .RetryAsync(
				1,
				async (ex, count) =>
				{
					await DistributedResourceManager.DiscardBidAsync(
						request.PlayerCode,
						cancellationToken).ConfigureAwait(false);

					_logger.LogWarning(ex, "NoReponders error retry {count}", count);
				})
		   .ExecuteAsync(
			   async ct =>
			   {
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
					   bidToken,
					   cancellationToken).ConfigureAwait(false);

				   return response;
			   },
			   cancellationToken).ConfigureAwait(false);
	}
}
