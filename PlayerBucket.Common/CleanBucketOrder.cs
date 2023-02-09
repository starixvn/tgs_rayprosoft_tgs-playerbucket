namespace Tgs.Service.PlayerBuckets;
public record CleanBucketOrder(
	Guid BrandId,
	Guid WalletId,
	decimal CurrentBalance,
	decimal CurrentBucketAmount,
	Guid BucketAnchorId,
	Guid DepositFormId,
	decimal ClearWageringRequirementBalance);
