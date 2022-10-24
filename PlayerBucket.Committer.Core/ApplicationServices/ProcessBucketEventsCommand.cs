namespace Tgs.PlayerBuckets.Committer.ApplicationServices;
public record ProcessBucketEventsCommand(Guid BucketId, string PlayerCode);
