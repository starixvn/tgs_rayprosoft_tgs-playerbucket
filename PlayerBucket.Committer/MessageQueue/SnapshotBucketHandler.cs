using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class SnapshotBucketHandler : IMessageHandler
{
	private readonly IEventSource _eventSource;

	public SnapshotBucketHandler(IEventSource eventSource)
	{
		_eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = SnapshotRequest.Parser.ParseFrom(data.Span);

		return _eventSource.PublishSnapshotRequestAsync(new SnapshotEvent(
			request.BucketId,
			request.PlayerCode), cancellationToken);
	}
}
