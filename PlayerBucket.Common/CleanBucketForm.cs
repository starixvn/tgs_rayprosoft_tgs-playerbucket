namespace Tgs.Service.PlayerBuckets;
public record CleanBucketForm(
	Guid Id,
	string PlayerCode,
	Guid BrandId,
	decimal CurrentBalance,
	decimal CurrentBucketAmount,
	Guid BucketAnchorId,
	Guid DepositFormId,
	CleanFlowStatus Status,
	DateTime CreatedOn);
