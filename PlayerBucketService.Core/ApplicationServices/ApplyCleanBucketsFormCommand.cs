namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record ApplyCleanBucketsFormCommand(
	string PlayerCode,
	Guid BrandId,
	Guid WalletId,
	decimal CurrentBalance,
	IEnumerable<TypeBucketResult> CurrentBucketAmounts,
	Guid DepositFormId,
	decimal ClearWageringRequirementBalance);
