using Tgs.MessageQueue;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class CreateBucketDispatchProcessor : IMessageProcessor
{
	private readonly IMessageSender _messageSender;

	public CreateBucketDispatchProcessor(IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var subject = $"tgs.bucket.worker.create.{Environment.MachineName}";

		return _messageSender.RequestAsync(subject, data, cancellationToken);
	}
}
