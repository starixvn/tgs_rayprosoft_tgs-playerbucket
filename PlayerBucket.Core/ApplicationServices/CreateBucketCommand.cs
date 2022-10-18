namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record CreateBucketCommand(string PlayerCode, decimal Amount, BucketType Type, string? ExternalId);
