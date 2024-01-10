using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.BucketCommands;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class CleanBucketHandler(IBucketDispatcher bucketDispatcher)
	: IMessageHandler<ReadOnlyMemory<byte>>
{
	public async ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = CleanBucketRequest.Parser.ParseFrom(data.Span);

		var externalId = string.IsNullOrWhiteSpace(request.ExternalId)
			? null
			: request.ExternalId;
		var remark = string.IsNullOrWhiteSpace(request.Remark)
			? null
			: request.Remark;

		var command = new CleanBucketsCommand(request.AnchorBucketId, externalId, remark);

		await bucketDispatcher.PushCommandAsync(
			request.PlayerCode,
			command,
			cancellationToken).ConfigureAwait(false);
	}
}
