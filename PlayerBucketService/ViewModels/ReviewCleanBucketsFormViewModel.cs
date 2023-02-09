using System.Text.Json;

namespace PlayerBucketService.ViewModels;

public class ReviewCleanBucketsFormViewModel
{
	public decimal CleaningBalance { get; set; }

	public decimal CleaningBucketAmount { get; set; }

	public JsonDocument? GameBalanceReturn { get; set; }
}
