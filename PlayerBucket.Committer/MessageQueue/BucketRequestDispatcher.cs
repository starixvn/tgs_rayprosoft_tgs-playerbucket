using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
using Polly;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public abstract class BucketRequestDispatcher(
	IMessageSender messageSender,
	IDistributedResourceManager distributedResourceManager,
	ILogger logger)
{
	private static readonly ResiliencePropertyKey<IDistributedResourceManager> _DistributedResourceManagerPropertyKey =
		new("DistributedResourceManager");

	private static readonly ResiliencePropertyKey<Guid> _BidBucketIdPropertyKey =
		new("BucketId");

	private static readonly ResiliencePropertyKey<ILogger> _LoggerPropertyKey =
		new("Logger");

	private static readonly ResiliencePipeline<string> _BidTokenPipeline = new ResiliencePipelineBuilder<string>()
		.AddRetry(new Polly.Retry.RetryStrategyOptions<string>
		{
			ShouldHandle = new PredicateBuilder<string>().Handle<NatsNoRespondersException>().Handle<NatsNoReplyException>(),
			MaxRetryAttempts = 1,
			OnRetry = async arg =>
			{
				if (!arg.Context.Properties.TryGetValue(_DistributedResourceManagerPropertyKey, out var mgr))
					throw new ArgumentException("找不到 DistributedResourceManager");
				if (!arg.Context.Properties.TryGetValue(_BidBucketIdPropertyKey, out var bucketId))
					throw new ArgumentException("找不到 BucketId");

				var logger = arg.Context.Properties.GetValue(_LoggerPropertyKey, NullLogger.Instance);

				await mgr.DiscardBidAsync(
					bucketId,
					arg.Context.CancellationToken).ConfigureAwait(false);

				logger.LogWarning(arg.Outcome.Exception, "NoReponders error retry {count}", arg.AttemptNumber);
			},
		})
		.Build();

	protected IMessageSender MessageSender { get; } = messageSender ?? throw new ArgumentNullException(nameof(messageSender));

	protected IDistributedResourceManager DistributedResourceManager { get; } = distributedResourceManager ?? throw new ArgumentNullException(nameof(distributedResourceManager));

	protected async Task<string> GetBidTokenAsync(Guid bucketId, CancellationToken cancellationToken)
	{
		var executeContext = ResilienceContextPool.Shared.Get(cancellationToken);
		try
		{
			executeContext.Properties.Set(_BidBucketIdPropertyKey, bucketId);
			executeContext.Properties.Set(_DistributedResourceManagerPropertyKey, DistributedResourceManager);
			executeContext.Properties.Set(_LoggerPropertyKey, logger);

			return await _BidTokenPipeline.ExecuteAsync(
				async ct => await InternalGetBidTokenAsync(
					bucketId,
					cancellationToken).ConfigureAwait(false),
				cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			ResilienceContextPool.Shared.Return(executeContext);
		}
	}

	private async ValueTask<string> InternalGetBidTokenAsync(Guid bucketId, CancellationToken cancellationToken)
	{
		var bidToken = await DistributedResourceManager
			.GetBidTokenAsync(bucketId, cancellationToken)
			.ConfigureAwait(false);

		if (string.IsNullOrEmpty(bidToken))
		{
			var bidResponseData = await MessageSender.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
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
