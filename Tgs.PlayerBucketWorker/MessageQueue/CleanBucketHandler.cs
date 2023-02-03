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

		var externalId = string.IsNullOrWhiteSpace(request.ExternalId)
			? null
			: request.ExternalId;
		var remark = string.IsNullOrWhiteSpace(request.Remark)
			? null
			: request.Remark;

		var command = new CleanBucketsCommand(request.AnchorBucketId, externalId, remark);

		await _bucketDispatcher.PushCommandAsync(
			request.PlayerCode,
			command,
			cancellationToken).ConfigureAwait(false);
	}
}
