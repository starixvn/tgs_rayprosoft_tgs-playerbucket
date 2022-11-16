using System.Text.Json.Serialization;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucketService.ViewModels;

public class PlayerBucketViewModel
{
	[JsonPropertyName("id")]
	public Guid BucketId { get; set; }

	[JsonPropertyName("type")]
	public BucketType BucketType { get; set; }

	public decimal Amount { get; set; }

	public decimal FillInAmount { get; set; }

	public DateTime CreatedOn { get; set; }

	public required string PlayerCode { get; set; }
}
