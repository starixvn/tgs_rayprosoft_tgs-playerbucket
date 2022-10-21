using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBuckets.Committer;

public interface ITgsRepository
{
	ValueTask InsertBucketEventsAsync(IEnumerable<BucketEvent> events, CancellationToken cancellationToken = default);
}
