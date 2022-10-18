using System.Text.Json;
using Json.Patch;

namespace Tgs.Service.PlayerBuckets;
public record FillInBucketInfo(
	decimal Amount,
	FillType Type,
	string? ExternalId,
	bool SetFull)
{
	public string CreateJsonPatch()
		=> JsonSerializer.Serialize(CreatePatch());

	public BucketInfo ApplyPatch(BucketInfo source)
		=> CreatePatch().Apply(source, PlayerBucketUtility.SerializerOptions)! with
		{
			FillInAmount = source.FillInAmount + Amount
		};

	private JsonPatch CreatePatch()
		=> SetFull
			? new { IsFull = false }.CreatePatch(new { IsFull = true }, PlayerBucketUtility.SerializerOptions)
			: new JsonPatch();
}
