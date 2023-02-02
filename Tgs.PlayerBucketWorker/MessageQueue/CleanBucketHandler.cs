using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.BucketCommands;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class CleanBucketHandler : IMessageHandler
{
	private readonly IBucketDispatcher _bucketDispatcher;

	public CleanBucketHandler(IBucketDispatcher bucketDispatcher)
	{
		_bucketDispatcher = bucketDispatcher ?? throw new ArgumentNullException(nameof(bucketDispatcher));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = CleanBucketRequest.Parser.ParseFrom(data.Span);

		var command = new CleanBucketsCommand(request.AnchorBucketId, request.ExternalId, null);

		await _bucketDispatcher.PushCommandAsync(
			request.PlayerCode,
			command,
			cancellationToken).ConfigureAwait(false);
	}
}
