using System.Runtime.CompilerServices;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.PlayerBuckets.Committer.ApplicationServices;

internal class ProcessBucketEventsCommandService : ICommandService<ProcessBucketEventsCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IBucketEventRepository _bucketEventRepository;
	private readonly IEventSource _eventSource;

	public ProcessBucketEventsCommandService(
		ITgsRepository tgsRepository,
		IBucketEventRepository bucketEventRepository,
		IEventSource eventSource)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_bucketEventRepository = bucketEventRepository ?? throw new ArgumentNullException(nameof(bucketEventRepository));
		_eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
	}

	public async ValueTask ExecuteAsync(ProcessBucketEventsCommand command, CancellationToken cancellationToken = default)
	{
		await foreach (var waitPersistentEvents in GetWaitingProcessBatchAsync(command.BucketId, cancellationToken))
		{
			if (waitPersistentEvents.Length == 0)
				break;

			await _tgsRepository.InsertBucketEventsAsync(
				waitPersistentEvents,
				cancellationToken).ConfigureAwait(false);

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
