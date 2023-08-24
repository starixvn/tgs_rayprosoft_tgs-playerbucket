using System.Text.Json;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record CompleteCleanBucketsFormCommand(
	Guid FormId,
	decimal CleaningBalance,
	IEnumerable<CleanFormBucketResult> CleaningBucketAmounts,
	JsonDocument? GameBalanceReturn);
