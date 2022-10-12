using System.Text.Json;
using Json.Patch;

namespace Tgs.Service.PlayerBuckets;
public record FillInBucketInfo(
	decimal Amount,
	FillType Type,
	string? ExternalId,
	bool SetFull)
{
	private static JsonPatch _EmptyPatch = new JsonPatch();
	private static JsonPatch _SetFullPatch = new { IsFull = false }.CreatePatch(new { IsFull = true }, PlayerBucketUtility.SerializerOptions);

	public string CreateJsonPatch()
	{
		var patch = SetFull ? _SetFullPatch : _EmptyPatch;

		return JsonSerializer.Serialize(patch);
	}
}
