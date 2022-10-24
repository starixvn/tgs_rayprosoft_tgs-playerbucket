using Google.Protobuf;
using NATS.Client;
using Polly;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public abstract class BucketRequestDispatcher
{
	private readonly ILogger _logger;

	protected IMessageSender MessageSender { get; }

	protected IDistributedResourceManager DistributedResourceManager { get; }

	protected BucketRequestDispatcher(IMessageSender messageSender, IDistributedResourceManager distributedResourceManager, ILogger logger)
	{
		MessageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
		DistributedResourceManager = distributedResourceManager ?? throw new ArgumentNullException(nameof(distributedResourceManager));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected Task<string> GetBidTokenAsync(string playerCode, CancellationToken cancellationToken)
		=> Policy
			.Handle<NATSNoRespondersException>()
			.RetryAsync(
				1,
				async (ex, count) =>
				{
					await DistributedResourceManager.DiscardBidAsync(
						playerCode,
						cancellationToken).ConfigureAwait(false);

					_logger.LogWarning(ex, "NoReponders error retry {count}", count);
				})
			.ExecuteAsync(
				async ct => await InternalGetBidTokenAsync(
					playerCode,
					cancellationToken).ConfigureAwait(false),
				cancellationToken);

	protected ValueTask ExtendBidAsync(string playerCode, CancellationToken cancellationToken)
		=> DistributedResourceManager.ExtendBidAsync(playerCode, cancellationToken);

	private async ValueTask<string> InternalGetBidTokenAsync(string playerCode, CancellationToken cancellationToken)
	{
		var bidToken = await DistributedResourceManager
			.GetBidTokenAsync(playerCode, cancellationToken)
			.ConfigureAwait(false);

		if (string.IsNullOrEmpty(bidToken))
		{
			var bidResponseData = await MessageSender.RequestAsync(
				"tgs.bucket.bid",
				new BidRequest
				{
					PlayerCode = playerCode
				}.ToByteArray(),
				cancellationToken)
				.ConfigureAwait(false);

			var bidResponse = BidResponse.Parser.ParseFrom(bidResponseData.Span);

			bidToken = bidResponse.Token;
		}

		return bidToken;
	}
}
