using System.Text.Json;
using Json.Patch;
using Microsoft.Extensions.Logging;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.PlayerBuckets.Committer.ApplicationServices;

public class SnapshotPlayerBucketCommandService(
	ITgsRepository tgsRepository,
	IBucketEventRepository bucketEventRepository,
	ILogger<SnapshotPlayerBucketCommandService> logger)
	: ICommandService<SnapshotPlayerBucketCommand>
{
	public async ValueTask ExecuteAsync(SnapshotPlayerBucketCommand command, CancellationToken cancellationToken = default)
	{
		var existsBucketSnapshot = await tgsRepository.GetCreatedBucketSnapshotAsync(
			command.BucketId,
			cancellationToken).ConfigureAwait(false);

		var allBucketEvents = existsBucketSnapshot switch
		{
			null => tgsRepository.GetBucketAllEventsAsync(command.BucketId, cancellationToken),
			not null when existsBucketSnapshot.SnapshotId is null => tgsRepository.GetBucketAllEventsAsync(
				existsBucketSnapshot.Id,
				cancellationToken),
			not null => tgsRepository.GetBucketAllEventsAsync(
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
			await tgsRepository.UpdatePlayerBucketAsync(
				snapshotBucket! with
				{
					SnapshotId = snapshotId,
					FillInAmount = Math.Abs(usedAmount)
				},
				cancellationToken).ConfigureAwait(false);
			await bucketEventRepository.DeleteCreatedBucketAsync(
				command.PlayerCode,
				command.BucketId,
				cancellationToken).ConfigureAwait(false);
		}
		else
			logger.LogWarning(
				"Bucket: {BucketId}, PlayerCode: {PlayerCode}, dotn't update snapshot.",
				command.BucketId,
				command.PlayerCode);
	}
}
