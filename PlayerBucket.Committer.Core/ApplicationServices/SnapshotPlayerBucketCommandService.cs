using System.Text.Json;
using Json.Patch;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.PlayerBuckets.Committer.ApplicationServices;

public class SnapshotPlayerBucketCommandService : ICommandService<SnapshotPlayerBucketCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IBucketEventRepository _bucketEventRepository;

	public SnapshotPlayerBucketCommandService(ITgsRepository tgsRepository, IBucketEventRepository bucketEventRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_bucketEventRepository = bucketEventRepository ?? throw new ArgumentNullException(nameof(bucketEventRepository));
	}

	public async ValueTask ExecuteAsync(SnapshotPlayerBucketCommand command, CancellationToken cancellationToken = default)
	{
		var existsBucketSnapshot = await _tgsRepository.GetCreatedBucketSnapshotAsync(
			command.BucketId,
			cancellationToken).ConfigureAwait(false);

		var allBucketEvents = existsBucketSnapshot switch
		{
			null => _tgsRepository.GetBucketAllEventsAsync(command.BucketId, cancellationToken),
			not null when existsBucketSnapshot.SnapshotId is null => _tgsRepository.GetBucketAllEventsAsync(
				existsBucketSnapshot.Id,
				cancellationToken),
			not null => _tgsRepository.GetBucketAllEventsAsync(
				existsBucketSnapshot.Id,
				existsBucketSnapshot.SnapshotId.Value,
				cancellationToken)
		};

		var snapshotId = existsBucketSnapshot?.SnapshotId ?? Guid.Empty;
		var usedAmount = -existsBucketSnapshot?.FillInAmount ?? 0M;
		var snapshotBucket = existsBucketSnapshot;

		await foreach (var evt in allBucketEvents
			.WithCancellation(cancellationToken)
			.ConfigureAwait(false))
		{
			var evtPatch = evt.ChangeSets.Deserialize<JsonPatch>()!;
			snapshotBucket = evtPatch.Apply(snapshotBucket, PlayerBucketUtility.SerializerOptions);

			snapshotId = evt.Id;
			if (evt.ChangeAmount < 0M)
				usedAmount += evt.ChangeAmount;
		}

		if (snapshotId != Guid.Empty && snapshotId.CompareTo(existsBucketSnapshot?.SnapshotId ?? Guid.Empty) > 0)
		{
			await _tgsRepository.UpdatePlayerBucketAsync(
				snapshotBucket! with
				{
					SnapshotId = snapshotId,
					FillInAmount = Math.Abs(usedAmount)
				},
				cancellationToken).ConfigureAwait(false);
			await _bucketEventRepository.DeleteCreatedBucketAsync(
				command.PlayerCode,
				command.BucketId,
				cancellationToken).ConfigureAwait(false);
		}
	}
}
