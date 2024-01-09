using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class SnapshotBucketHandler : IMessageHandler<ReadOnlyMemory<byte>>
{
	private readonly IEventSource _eventSource;

	public SnapshotBucketHandler(IEventSource eventSource)
	{
		_eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
	}

	public ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = SnapshotRequest.Parser.ParseFrom(data.Span);

		return _eventSource.PublishSnapshotRequestAsync(new SnapshotEvent(
			request.BucketId,
			request.PlayerCode), cancellationToken);
	}

}
