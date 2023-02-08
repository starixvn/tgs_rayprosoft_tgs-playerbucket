namespace Tgs.Service.PlayerBuckets;
public record CompleteCleanBucketRequest(
	decimal CleaningBalance,
	decimal CleaningBucketAmount);
