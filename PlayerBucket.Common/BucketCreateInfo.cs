using System.Text.Json;
using Json.Patch;
using Json.Pointer;

namespace Tgs.Service.PlayerBuckets;
public record BucketCreateInfo(string PlayerCode, decimal Amount, BucketType Type, string? ExternalId)
{
	public JsonDocument CreateJsonPatchDocument(Guid bucketId)
		=> JsonSerializer.SerializeToDocument(CreateJsonPatch(bucketId));

	public JsonPatch CreateJsonPatch(Guid bucketId)
		=> new(
			PatchOperation.Add(
				JsonPointer.Create<BucketInfo>(o => o),
				JsonSerializer.SerializeToNode(new
				{
					Id = bucketId,
					PlayerCode,
					Amount,
					Type
				}, PlayerBucketUtility.SerializerOptions)));
}
