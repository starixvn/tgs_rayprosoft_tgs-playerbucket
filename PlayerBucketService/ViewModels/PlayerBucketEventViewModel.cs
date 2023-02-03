using Tgs.Service.PlayerBuckets;

namespace PlayerBucketService.ViewModels;

public class PlayerBucketEventViewModel
{
	public Guid Id { get; set; }

	public Guid BucketId { get; set; }

	public decimal ChangeAmount { get; set; }

	public FillType Type { get; set; }

	public string? ExternalId { get; set; }

	public DateTime CreatedOn { get; set; }

	public string? Remark { get; set; }
}
