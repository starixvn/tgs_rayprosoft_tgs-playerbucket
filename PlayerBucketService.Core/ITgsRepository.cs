using Tgs.Service.PlayerBuckets;

namespace Tgs.Service.PlayerBuckets;

public interface ITgsRepository
{
	IAsyncEnumerable<BucketInfo> QueryPlayerBucketsAsync(string playerCode, CancellationToken cancellationToken = default);
}
