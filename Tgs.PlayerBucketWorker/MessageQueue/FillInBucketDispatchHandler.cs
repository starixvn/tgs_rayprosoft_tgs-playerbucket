using NATS.Client;
using Polly;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class FillInBucketDispatchHandler : BucketRequestDispatcher, IMessageHandler
{
	private readonly ILogger<FillInBucketDispatchHandler> _logger;

	public FillInBucketDispatchHandler(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<FillInBucketDispatchHandler> logger)
		: base(messageSender, distributedResourceManager, logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = FillInRequest.Parser.ParseFrom(data.Span);

		await Policy
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

					var subject = $"tgs.bucket.worker.fill.{bidToken}";

					await MessageSender.SendAsync(
						subject,
						data,
						cancellationToken).ConfigureAwait(false);

					await ExtendBidAsync(
						request.PlayerCode,
						bidToken,
						cancellationToken).ConfigureAwait(false);
				},
				cancellationToken).ConfigureAwait(false);
	}
}
