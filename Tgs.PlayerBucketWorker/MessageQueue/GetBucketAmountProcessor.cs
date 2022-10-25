using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class GetBucketAmountProcessor : IMessageProcessor
{
	private readonly IBucketDispatcher _bucketDispatcher;

	public GetBucketAmountProcessor(IBucketDispatcher bucketDispatcher)
	{
		_bucketDispatcher = bucketDispatcher ?? throw new ArgumentNullException(nameof(bucketDispatcher));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = GetBucketAmountRequest.Parser.ParseFrom(data.Span);

		var playerCode = request.PlayerCode;

		var command = new ReadBucketAmountCommand();

		await _bucketDispatcher.PushCommandAsync(
			playerCode,
			command,
			cancellationToken).ConfigureAwait(false);

		var amount = await command.GetAmountAsync().ConfigureAwait(false);

		var response = new GetBucketAmountResponse
		{
			Amount = amount
		};

		return response.ToByteArray();
	}
}
