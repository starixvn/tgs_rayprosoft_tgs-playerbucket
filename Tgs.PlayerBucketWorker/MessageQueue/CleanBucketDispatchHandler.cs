using NATS.Client;
using Polly;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class CleanBucketDispatchHandler : BucketRequestDispatcher, IMessageHandler
{
	private readonly ILogger<CleanBucketDispatchHandler> _logger;

	public CleanBucketDispatchHandler(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<CleanBucketDispatchHandler> logger) : base(messageSender, distributedResourceManager, logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = CleanBucketRequest.Parser.ParseFrom(data.Span);

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

					var subject = $"tgs.bucket.worker.clean.{bidToken}";

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
