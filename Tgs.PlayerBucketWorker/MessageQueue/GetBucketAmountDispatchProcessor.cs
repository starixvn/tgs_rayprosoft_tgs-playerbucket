using System.Data;
using NATS.Client;
using NATS.Client.Core;
using Polly;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class GetBucketAmountDispatchProcessor : BucketRequestDispatcher, IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	private readonly ILogger<GetBucketAmountDispatchProcessor> _logger;

	public GetBucketAmountDispatchProcessor(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger<GetBucketAmountDispatchProcessor> logger) : base(messageSender, distributedResourceManager, logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = GetBucketAmountRequest.Parser.ParseFrom(data.Span);

		return await Policy
			.Handle<NatsNoRespondersException>()
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

					var subject = $"tgs.bucket.worker.amount.{bidToken}";

					var response = await MessageSender.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
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
