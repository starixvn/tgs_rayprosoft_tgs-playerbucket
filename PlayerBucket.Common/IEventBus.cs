namespace Tgs.Service.PlayerBuckets;

public interface IEventBus
{
	IObservable<BucketChangedEvent> BucketChangedObservable { get; }
}
