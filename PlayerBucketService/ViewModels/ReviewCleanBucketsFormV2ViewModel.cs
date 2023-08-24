using System.Text.Json;

namespace PlayerBucketService.ViewModels;

public class ReviewCleanBucketsFormV2ViewModel
{
	public required decimal CleaningBalance { get; set; }

	public required IEnumerable<CleanBucketResultViewModel> CleaningBucketAmounts { get; set; }

	public JsonDocument? GameBalanceReturn { get; set; }
}
