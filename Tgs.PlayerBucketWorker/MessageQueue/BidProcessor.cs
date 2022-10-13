using Tgs.MessageQueue;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class BidProcessor : IMessageProcessor
{
	public ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}
}
