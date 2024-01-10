using Google.Protobuf;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.BucketCommands;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class CreateBucketProcessor(IBucketDispatcher bucketDispatcher) : IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	private readonly IBucketDispatcher _bucketDispatcher = bucketDispatcher ?? throw new ArgumentNullException(nameof(bucketDispatcher));

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = CreateBucketRequest.Parser.ParseFrom(data.Span);

		var playerCode = request.PlayerCode;
		var amount = request.Amount.ToDecimal() ?? 0M;
		var bucketType = Enum.Parse<BucketType>(request.BucketType);
		var externalId = string.IsNullOrWhiteSpace(request.ExternalId)
			? null
			: request.ExternalId;
		var remark = string.IsNullOrWhiteSpace(request.Remark)
			? null
			: request.Remark;

		var command = new CreateBucketCommand(amount, bucketType, externalId, remark);

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
