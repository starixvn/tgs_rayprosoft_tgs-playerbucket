namespace Tgs.Service.PlayerBuckets;

public interface IEventSource
{
	ValueTask PublishBucketChangedAsync(BucketChangedEvent evt, CancellationToken cancellationToken = default);
}
