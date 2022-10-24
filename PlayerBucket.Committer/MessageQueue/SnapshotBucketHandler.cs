using Tgs.MessageQueue;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class SnapshotBucketHandler : IMessageHandler
{
	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}
}
