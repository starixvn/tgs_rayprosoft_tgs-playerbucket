using Tgs.MessageQueue;

namespace Tgs.PlayerBucketService.MessageQueue;

public class CreateBucketProcessor : IMessageProcessor
{
	public ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
