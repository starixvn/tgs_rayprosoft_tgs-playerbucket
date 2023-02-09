namespace Tgs.Service.PlayerBuckets;
public record CleanBucketForm(
	Guid Id,
	string PlayerCode,
	Guid WalletId,
	Guid BrandId,
	decimal CurrentBalance,
	decimal CurrentBucketAmount,
	Guid BucketAnchorId,
	Guid DepositFormId,
	decimal ClearWageringRequirementBalance,
	decimal? CleaningBalance,
	decimal? CleaningBucketAmount,
	CleanFlowStatus Status,
	DateTime CreatedOn);
