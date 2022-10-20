namespace Tgs.Service.PlayerBuckets;

internal interface IEventSource
{
	ValueTask PublishBucketChangedAsync(BucketChangedEvent evt, CancellationToken cancellationToken = default);
}
