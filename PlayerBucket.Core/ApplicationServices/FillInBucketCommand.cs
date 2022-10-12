namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record FillInBucketCommand(
	string PlayerCode,
	decimal Amount,
	FillType Type,
	string? ExternalId);
