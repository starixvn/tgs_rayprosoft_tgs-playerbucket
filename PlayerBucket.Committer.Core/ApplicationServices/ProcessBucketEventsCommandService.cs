using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.PlayerBuckets.Committer.ApplicationServices;

internal class ProcessBucketEventsCommandService : ICommandService<ProcessBucketEventsCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IBucketEventRepository _bucketEventRepository;
	private readonly IEventSource _eventSource;
	private readonly ILogger<ProcessBucketEventsCommandService> _logger;

	public ProcessBucketEventsCommandService(
		ITgsRepository tgsRepository,
		IBucketEventRepository bucketEventRepository,
		IEventSource eventSource,
		ILogger<ProcessBucketEventsCommandService> logger)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_bucketEventRepository = bucketEventRepository ?? throw new ArgumentNullException(nameof(bucketEventRepository));
		_eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(ProcessBucketEventsCommand command, CancellationToken cancellationToken = default)
	{
		await foreach (var waitPersistentEvents in GetWaitingProcessBatchAsync(command.BucketId, cancellationToken))
		{
			if (waitPersistentEvents.Length == 0)
				break;

			try
			{
				await _tgsRepository.InsertBucketEventsAsync(
					waitPersistentEvents,
					cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "{waitPersistentEvents}", waitPersistentEvents);

				await _tgsRepository.MigrateBucketEventsAsync(
					waitPersistentEvents,
					cancellationToken).ConfigureAwait(false);
			}

			await _bucketEventRepository.RemoveEventsAsync(
				command.BucketId,
				waitPersistentEvents.Length,
				cancellationToken).ConfigureAwait(false);
		}

		var snapshotEvent = new SnapshotEvent(command.BucketId, command.PlayerCode);
		await _eventSource.PublishSnapshotRequestAsync(
			snapshotEvent,
			cancellationToken).ConfigureAwait(false);
	}

	public async IAsyncEnumerable<BucketEvent[]> GetWaitingProcessBatchAsync(
		Guid bucketId,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		while (true)
		{
			yield return await _bucketEventRepository
				.GetWaitingInsertEventsAsync(bucketId, cancellationToken)
				.ToArrayAsync(cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
