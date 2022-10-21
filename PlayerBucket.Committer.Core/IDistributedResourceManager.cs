namespace Tgs.PlayerBuckets.Committer;

public interface IDistributedResourceManager
{
	ValueTask<string> BidAsync(Guid bucketId, string token, CancellationToken cancellationToken = default);

	ValueTask<string?> GetBidTokenAsync(Guid bucketId, CancellationToken cancellationToken = default);

	ValueTask DiscardBidAsync(Guid bucketId, CancellationToken cancellationToken = default);
}
