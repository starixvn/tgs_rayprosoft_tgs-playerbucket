namespace Tgs.Service.PlayerBuckets.ApplicationServices;

public record GetPlayerBucketHistoriesByDateQuery(Guid BucketId, DateTime QueryDate);
