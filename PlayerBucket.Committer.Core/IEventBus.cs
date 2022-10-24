using Tgs.PlayerBuckets.Committer;

namespace Tgs.Service.PlayerBuckets;

internal interface IEventBus
{
	IObservable<BucketChangedEvent> BucketChangedObservable { get; }

	IObservable<SnapshotEvent> SnapshotObservable { get; }
}
