using System.Text.Json;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;
public record RejectCleanBucketsFormCommand(
	Guid FormId,
	decimal CleaningBalance,
	decimal CleaningBucketAmount,
	JsonDocument? GameBalanceReturn);
