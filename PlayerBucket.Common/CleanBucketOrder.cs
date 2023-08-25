namespace Tgs.Service.PlayerBuckets;
public record CleanBucketOrder(
	Guid BrandId,
	Guid WalletId,
	decimal CurrentBalance,
	IEnumerable<TypeBucketResult> CurrentBucketAmounts,
	Guid DepositFormId,
	decimal ClearWageringRequirementBalance);
