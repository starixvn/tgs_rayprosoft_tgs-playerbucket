using Tgs.Service.PlayerBuckets;

namespace PlayerBucketService.ViewModels;

public class PlayerBucketViewModel
{
	public Guid BucketId { get; set; }

	public BucketType BucketType { get; set; }

	public decimal Amount { get; set; }

	public decimal FillInAmount { get; set; }

	public DateTime CreatedOn { get; set; }
}
