using MongoDB.Bson.Serialization;
using Tgs.MessageQueue;
using Tgs.PlayerBuckets.Committer.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class ProcessBucketEventRequestHandler : IMessageHandler
{
	private readonly ICommandService<ProcessBucketEventsCommand> _processService;
	private readonly ILogger<ProcessBucketEventRequestHandler> _logger;

	public ProcessBucketEventRequestHandler(ICommandService<ProcessBucketEventsCommand> processService, ILogger<ProcessBucketEventRequestHandler> logger)
	{
		_processService = processService ?? throw new ArgumentNullException(nameof(processService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		try
		{
			var eventData = BsonSerializer.Deserialize<BucketChangedEvent>(data.ToArray());

			var command = new ProcessBucketEventsCommand(eventData.BucketId);

			await _processService.ExecuteAsync(
				command,
				cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "error");
		}
	}
}
