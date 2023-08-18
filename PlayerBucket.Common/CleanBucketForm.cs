namespace Tgs.Service.PlayerBuckets;
public record CleanBucketForm(
	Guid Id,
	string PlayerCode,
	Guid WalletId,
	Guid BrandId,
	decimal CurrentBalance,
	IEnumerable<KeyValuePair<string?, ReadBucketResult>> CurrentBucketAmounts,
	Guid DepositFormId,
	decimal ClearWageringRequirementBalance,
	decimal? CleaningBalance,
	IEnumerable<KeyValuePair<string?, ReadBucketResult>>? CleaningBucketAmounts,
	CleanFlowStatus Status,
	DateTime CreatedOn);
