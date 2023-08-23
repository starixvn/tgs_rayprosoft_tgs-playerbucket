using System.Text.Json.Serialization;

namespace PlayerBucketService.ViewModels;

public class ReadBucketResultViewModel
{
	[JsonPropertyName("amount")]
	public required decimal BucketAmount { get; set; }

	public Guid? AnchorBucketId { get; set; }
}
