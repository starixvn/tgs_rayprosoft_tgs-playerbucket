namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record ApplyCleanBucketsFormCommand(
	string PlayerCode,
	Guid BrandId,
	Guid WalletId,
	Guid BucketAnchorId,
	decimal CurrentBalance,
	decimal CurrentBucketAmount,
	Guid DepositFormId,
	decimal ClearWageringRequirementBalance);
