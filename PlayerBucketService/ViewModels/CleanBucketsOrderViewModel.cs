namespace PlayerBucketService.ViewModels;

public class CleanBucketsOrderViewModel
{
	public Guid BrandId { get; set; }

	public Guid BucketAnchorId { get; set; }

	public decimal CurrentBalance { get; set; }

	public decimal CurrentBucketAmount { get; set; }

	public Guid DepositFormId { get; set; }
}
