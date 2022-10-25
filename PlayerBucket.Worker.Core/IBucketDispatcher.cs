namespace Tgs.Service.PlayerBuckets;

public interface IBucketDispatcher
{
	Task PushCommandAsync(string playerCode, IBucketCommand command, CancellationToken cancellationToken = default);

	void ExtendExpiry(string playerCode);
}
