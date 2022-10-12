using System.Text.Json;

namespace Tgs.Service.PlayerBuckets;
public record BucketEvent(
	Guid Id,
	Guid BucketId,
	decimal ChangeAmount,
	FillType Type,
	string? ExternalId,
	JsonDocument ChangeSets,
	DateTime CreatedOn);
