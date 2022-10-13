using System.Text.Json;
using Json.Patch;
using Json.Pointer;

namespace Tgs.Service.PlayerBuckets;
public record BucketCreateInfo(string PlayerCode, decimal Amount, BucketType Type, string? ExternalId)
{
	public string CreateJsonPatch(Guid bucketId)
	{
		var patch = new JsonPatch(
			PatchOperation.Add(
				JsonPointer.Create<BucketInfo>(o => o),
				JsonSerializer.SerializeToNode(new
				{
					Id = bucketId,
					PlayerCode,
					Amount,
					Type
				}, PlayerBucketUtility.SerializerOptions)));

		return JsonSerializer.Serialize(patch);
	}
}
