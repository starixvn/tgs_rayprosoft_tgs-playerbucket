namespace Tgs.Service.PlayerBuckets;

public interface IBucketCommand
{
	ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default);
}
