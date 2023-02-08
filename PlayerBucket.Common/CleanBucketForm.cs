namespace Tgs.Service.PlayerBuckets;
public record CleanBucketForm(
	Guid Id,
	string PlayerCode,
	Guid BrandId,
	decimal CurrentBalance,
	decimal CurrentBucketAmount,
	Guid BucketAnchorId,
	Guid DepositFormId,
	decimal? CleaningBalance,
	decimal? CleaningBucketAmount,
	CleanFlowStatus Status,
	DateTime CreatedOn);
