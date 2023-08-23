namespace Tgs.Service.PlayerBuckets;
public record CleanBucketForm(
	Guid Id,
	string PlayerCode,
	Guid WalletId,
	Guid BrandId,
	decimal CurrentBalance,
	IEnumerable<CleanFormBucketResult> CurrentBucketAmounts,
	Guid DepositFormId,
	decimal ClearWageringRequirementBalance,
	decimal? CleaningBalance,
	IEnumerable<CleanFormBucketResult>? CleaningBucketAmounts,
	CleanFlowStatus Status,
	DateTime CreatedOn);
