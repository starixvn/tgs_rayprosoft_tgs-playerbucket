using System.Text.Json;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record RejectCleanBucketsFormCommand(
	Guid FormId,
	decimal CleaningBalance,
	IEnumerable<CleanFormBucketResult> CleaningBucketAmounts,
	JsonDocument? GameBalanceReturn);
