using System.Text.Json;
using Json.More;
using Json.Patch;

namespace Tgs.Service.PlayerBuckets;
public record CleanPlayerBucketForm(
	Guid Id,
	string PlayerCode,
	Guid BrandId,
	Guid WalletId,
	decimal CurrentBalance,
	decimal CurrentBucketAmount,
	Guid BucketAnchorId,
	Guid DepositFormId,
	decimal ClearWageringRequirementBalance,
	decimal? CleaningBalance,
	decimal? CleaningBucketAmount,
	JsonDocument? GameBalanceReturn,
	CleanFlowStatus Status,
	DateTime CreatedOn)
{
	public JsonPatch CreateJsonPatch(CleanPlayerBucketForm? baseData)
		=> baseData.CreatePatch(this, PlayerBucketUtility.SerializerOptions);

	public JsonDocument CreateJsonPatchDocument(CleanPlayerBucketForm? baseData)
		=> CreateJsonPatch(baseData).ToJsonDocument();
}
