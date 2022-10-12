using System.Text.Json;
using Json.Patch;
using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

public class SnapshotPlayerBucketCommandService : ICommandService<SnapshotPlayerBucketCommand>
{
	private readonly ITgsRepository _tgsRepository;

	public SnapshotPlayerBucketCommandService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}
	public async ValueTask ExecuteAsync(SnapshotPlayerBucketCommand command, CancellationToken cancellationToken = default)
	{
		var allBucketEvents = _tgsRepository.GetBucetAllEventsAsync(command.BucketId, cancellationToken);

		var snapshotId = Guid.Empty;
		var usedAmount = 0M;
		var snapshotBucket = default(BucketInfo);

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

		await _tgsRepository.UpdatePlayerBucketAsync(
			snapshotBucket! with
			{
				SnapshotId = snapshotId
			},
			Math.Abs(usedAmount),
			cancellationToken).ConfigureAwait(false);
	}
}
