namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record ApplyCleanBucketsFormCommand(
	string PlayerCode,
	Guid BrandId,
	Guid BucketAnchorId,
	decimal CurrentBalance,
	decimal CurrentBucketAmount,
	Guid DepositFormId);
