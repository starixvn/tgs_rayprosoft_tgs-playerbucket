namespace Tgs.Service.PlayerBuckets;

public record BucketInfo(
	Guid Id,
	string PlayerCode,
	decimal Amount,
	BucketType Type,
	decimal FillInAmount,
	bool IsFull,
	Guid? SnapshotId);
