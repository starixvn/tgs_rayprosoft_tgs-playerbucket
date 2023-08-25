using System.Text.Json;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record CompleteCleanBucketsFormCommand(
	Guid FormId,
	decimal CleaningBalance,
	IEnumerable<TypeBucketResult> CleaningBucketAmounts,
	JsonDocument? GameBalanceReturn);
