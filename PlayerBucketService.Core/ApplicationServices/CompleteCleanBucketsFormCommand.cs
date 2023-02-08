namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record CompleteCleanBucketsFormCommand(
	Guid FormId,
	decimal CleaningBalance,
	decimal CleaningBucketAmount);
