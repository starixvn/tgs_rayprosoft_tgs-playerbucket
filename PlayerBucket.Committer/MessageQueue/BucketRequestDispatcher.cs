using Google.Protobuf;
using NATS.Client;
using Polly;
using Tgs.MessageQueue;
using Tgs.PlayerBuckets.Committer;
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

	protected Task<string> GetBidTokenAsync(Guid bucketId, CancellationToken cancellationToken)
		=> Policy
			.Handle<NATSNoRespondersException>()
			.RetryAsync(
				1,
				async (ex, count) =>
				{
					await DistributedResourceManager.DiscardBidAsync(
						bucketId,
						cancellationToken).ConfigureAwait(false);

					_logger.LogWarning(ex, "NoReponders error retry {count}", count);
				})
			.ExecuteAsync(
				async ct => await InternalGetBidTokenAsync(
					bucketId,
					cancellationToken).ConfigureAwait(false),
				cancellationToken);

	private async ValueTask<string> InternalGetBidTokenAsync(Guid bucketId, CancellationToken cancellationToken)
	{
		var bidToken = await DistributedResourceManager
			.GetBidTokenAsync(bucketId, cancellationToken)
			.ConfigureAwait(false);

		if (string.IsNullOrEmpty(bidToken))
		{
			var bidResponseData = await MessageSender.RequestAsync(
				"tgs.bucket.commit.bid",
				new CommitBidRequest
				{
					BucketId = bucketId
				}.ToByteArray(),
				cancellationToken)
				.ConfigureAwait(false);

			var bidResponse = BidResponse.Parser.ParseFrom(bidResponseData.Span);

			bidToken = bidResponse.Token;
		}

		return bidToken;
	}
}
