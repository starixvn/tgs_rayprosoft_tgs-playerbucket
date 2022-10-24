namespace Tgs.Service.PlayerBuckets;

public interface IDistributedResourceManager
{
	ValueTask<string> BidAsync(string playerCode, string token, CancellationToken cancellationToken = default);

	ValueTask<string?> GetBidTokenAsync(string playerCode, CancellationToken cancellationToken = default);

	ValueTask DiscardBidAsync(string playerCode, CancellationToken cancellationToken = default);

	ValueTask ExtendBidAsync(string playerCode, CancellationToken cancellationToken = default);
}
