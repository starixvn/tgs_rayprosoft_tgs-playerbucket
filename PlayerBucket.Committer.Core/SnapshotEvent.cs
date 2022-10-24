namespace Tgs.PlayerBuckets.Committer;
public record SnapshotEvent(Guid BucketId, string PlayerCode);
