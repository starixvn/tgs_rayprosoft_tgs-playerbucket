using Tgs.ServiceUtility;

namespace Tgs.PlayerBuckets.Committer.ApplicationServices;

internal class ProcessBucketEventsCommandService : ICommandService<ProcessBucketEventsCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IBucketEventRepository _bucketEventRepository;

	public ProcessBucketEventsCommandService(ITgsRepository tgsRepository, IBucketEventRepository bucketEventRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_bucketEventRepository = bucketEventRepository ?? throw new ArgumentNullException(nameof(bucketEventRepository));
	}

	public async ValueTask ExecuteAsync(ProcessBucketEventsCommand command, CancellationToken cancellationToken = default)
	{
		var waitPersistentEvents = await _bucketEventRepository
			.GetBucketAllEventsAsync(command.BucketId, cancellationToken)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		await _tgsRepository.InsertBucketEventsAsync(
			waitPersistentEvents,
			cancellationToken).ConfigureAwait(false);

		await _bucketEventRepository.RemoveEventsAsync(
			command.BucketId,
			waitPersistentEvents.Length,
			cancellationToken).ConfigureAwait(false);
	}
}
