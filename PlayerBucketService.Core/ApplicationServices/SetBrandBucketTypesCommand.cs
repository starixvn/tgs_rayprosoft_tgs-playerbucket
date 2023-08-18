namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record SetBrandBucketTypesCommand(Guid BrandId, IEnumerable<string> BucketTypes);
