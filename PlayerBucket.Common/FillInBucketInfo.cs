using System.Text.Json;
using Json.Patch;

namespace Tgs.Service.PlayerBuckets;
public record FillInBucketInfo(
	string PlayerCode,
	decimal Amount,
	FillType Type,
	string? ExternalId,
	bool SetFull)
{
	public JsonDocument CreateJsonPatchDocument()
		=> JsonSerializer.SerializeToDocument(CreateJsonPatch());

	public BucketInfo ApplyPatch(BucketInfo source)
		=> CreateJsonPatch().Apply(source, PlayerBucketUtility.SerializerOptions)! with
		{
			FillInAmount = source.FillInAmount + Amount
		};

	public JsonPatch CreateJsonPatch()
		=> SetFull
			? new { IsFull = false }.CreatePatch(new { IsFull = true }, PlayerBucketUtility.SerializerOptions)
			: new JsonPatch();
}
