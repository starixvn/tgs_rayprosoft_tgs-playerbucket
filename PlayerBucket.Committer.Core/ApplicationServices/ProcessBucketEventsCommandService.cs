using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.PlayerBuckets.Committer.ApplicationServices;

internal class ProcessBucketEventsCommandService(
	ITgsRepository tgsRepository,
	IBucketEventRepository bucketEventRepository,
	IEventSource eventSource,
	ILogger<ProcessBucketEventsCommandService> logger)
	: ICommandService<ProcessBucketEventsCommand>
{
	public async ValueTask ExecuteAsync(ProcessBucketEventsCommand command, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Process {BucketId}, {PlayerCode}", command.BucketId, command.PlayerCode);

		await foreach (var waitPersistentEvents in GetWaitingProcessBatchAsync(command.BucketId, cancellationToken))
		{
			if (waitPersistentEvents.Length == 0)
				break;

			var processCount = waitPersistentEvents.Length;
			try
			{
				await tgsRepository.InsertBucketEventsAsync(
					waitPersistentEvents,
					cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "{waitPersistentEvents}", waitPersistentEvents);

				await tgsRepository.MigrateBucketEventsAsync(
					waitPersistentEvents,
					cancellationToken).ConfigureAwait(false);
			}

			await bucketEventRepository.RemoveEventsAsync(
				command.BucketId,
				processCount,
				cancellationToken).ConfigureAwait(false);
		}

		var snapshotEvent = new SnapshotEvent(command.BucketId, command.PlayerCode);
		await eventSource.PublishSnapshotRequestAsync(
			snapshotEvent,
			cancellationToken).ConfigureAwait(false);
	}

	public async IAsyncEnumerable<BucketEvent[]> GetWaitingProcessBatchAsync(
		Guid bucketId,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		while (true)
		{
			yield return await bucketEventRepository
				.GetWaitingInsertEventsAsync(bucketId, cancellationToken)
				.ToArrayAsync(cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
