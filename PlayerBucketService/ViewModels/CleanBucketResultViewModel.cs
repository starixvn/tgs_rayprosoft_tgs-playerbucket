using System.Text.Json.Serialization;

namespace PlayerBucketService.ViewModels;

public class CleanBucketResultViewModel
{
	public string? BucketType { get; set; }

	[JsonPropertyName("result")]
	public required ReadBucketResultViewModel ReadBucketResult { get; set; }
}
