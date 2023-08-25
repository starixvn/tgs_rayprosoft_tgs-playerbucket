namespace Tgs.Service.PlayerBuckets;
public record CleanBucketForm(
	Guid Id,
	string PlayerCode,
	Guid WalletId,
	Guid BrandId,
	decimal CurrentBalance,
	IEnumerable<TypeBucketResult> CurrentBucketAmounts,
	Guid DepositFormId,
	decimal ClearWageringRequirementBalance,
	decimal? CleaningBalance,
	IEnumerable<TypeBucketResult>? CleaningBucketAmounts,
	CleanFlowStatus Status,
	DateTime CreatedOn);
