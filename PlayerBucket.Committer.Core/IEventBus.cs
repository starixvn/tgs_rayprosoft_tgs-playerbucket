using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBuckets.Committer;

internal interface IEventBus
{
	IObservable<BucketChangedEvent> BucketChangedObservable { get; }

	IObservable<SnapshotEvent> SnapshotObservable { get; }
}
