namespace Tgs.Service.PlayerBuckets;
public record CleanBucketOrder(
	Guid BrandId,
	decimal CurrentBalance,
	decimal CurrentBucketAmount,
	Guid BucketAnchorId,
	Guid DepositFormId);
