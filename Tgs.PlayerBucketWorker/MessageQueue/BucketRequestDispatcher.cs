using Google.Protobuf;
using NATS.Client.Core;
using Polly;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

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
			.Handle<NatsNoRespondersException>()
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

	protected async ValueTask ExtendBidAsync(string playerCode, string token, CancellationToken cancellationToken)
	{
		var isSuccess = await DistributedResourceManager.ExtendBidAsync(
			playerCode,
			cancellationToken).ConfigureAwait(false);

		if (isSuccess)
		{
			var extendRequest = new ExtendExpiryRequest
			{
				PlayerCode = playerCode
			};
			await MessageSender.PublishAsync(
				$"tgs.bucket.worker.expiry.{token}",
				extendRequest.ToByteArray(),
				cancellationToken).ConfigureAwait(false);
		}
	}

	private async ValueTask<string> InternalGetBidTokenAsync(string playerCode, CancellationToken cancellationToken)
	{
		var bidToken = await DistributedResourceManager
			.GetBidTokenAsync(playerCode, cancellationToken)
			.ConfigureAwait(false);

		if (string.IsNullOrEmpty(bidToken))
		{
			var bidResponseData = await MessageSender.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
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
