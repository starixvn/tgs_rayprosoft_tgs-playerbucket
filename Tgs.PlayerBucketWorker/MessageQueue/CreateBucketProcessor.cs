using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class CreateBucketProcessor : IMessageProcessor
{
	private readonly IBucketDispatcher _bucketDispatcher;

	public CreateBucketProcessor(IBucketDispatcher bucketDispatcher)
	{
		_bucketDispatcher = bucketDispatcher ?? throw new ArgumentNullException(nameof(bucketDispatcher));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = CreateBucketRequest.Parser.ParseFrom(data.Span);

		var playerCode = request.PlayerCode;
		var amount = request.Amount.ToDecimal() ?? 0M;
		var bucketType = Enum.Parse<BucketType>(request.BucketType);
		var externalId = string.IsNullOrWhiteSpace(request.ExternalId)
			? null
			: request.ExternalId;

		var command = new CreateBucketCommand(amount, bucketType, externalId);

		await _bucketDispatcher.PushCommandAsync(
			playerCode,
			command,
			cancellationToken).ConfigureAwait(false);

		var bucketId = await command.GetCreatedIdAsync().ConfigureAwait(false);

		var response = new CreateBucketResponse
		{
			BucketId = bucketId
		};

		return response.ToByteArray();
	}
}
