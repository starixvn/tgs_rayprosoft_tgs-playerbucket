using Tgs.MessageQueue;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class FillInBucketDispatchHandler : IMessageHandler
{
	private readonly IMessageSender _messageSender;

	public FillInBucketDispatchHandler(IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var subject = $"tgs.bucket.worker.fill.{Environment.MachineName}";

		return _messageSender.SendAsync(subject, data, cancellationToken);
	}
}
