namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record ApplyCleanBucketsFormCommand(
	string PlayerCode,
	Guid BrandId,
	Guid WalletId,
	decimal CurrentBalance,
	IEnumerable<CleanFormBucketResult> CurrentBucketAmounts,
	Guid DepositFormId,
	decimal ClearWageringRequirementBalance);
