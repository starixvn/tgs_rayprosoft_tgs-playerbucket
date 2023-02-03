using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.BucketCommands;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class FillInBucketHandler : IMessageHandler
{
	private readonly IBucketDispatcher _bucketDispatcher;

	public FillInBucketHandler(IBucketDispatcher bucketDispatcher)
	{
		_bucketDispatcher = bucketDispatcher ?? throw new ArgumentNullException(nameof(bucketDispatcher));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = FillInRequest.Parser.ParseFrom(data.Span);

		var playerCode = request.PlayerCode;
		var amount = request.Amount;
		var fillType = Enum.Parse<FillType>(request.FillType);
		var externalId = string.IsNullOrWhiteSpace(request.ExternalId)
			? null
			: request.ExternalId;
		var remark = string.IsNullOrWhiteSpace(request.Remark)
			? null
			: request.Remark;

		var command = new FillInBucketCommand(amount, fillType, externalId, remark);

		await _bucketDispatcher.PushCommandAsync(
			playerCode,
			command,
			cancellationToken).ConfigureAwait(true);

		await command.WaitAsync().ConfigureAwait(false);
	}
}
