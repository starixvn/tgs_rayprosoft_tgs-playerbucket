namespace PlayerBucketService.ViewModels;

public class ReadBucketResultViewModel
{
	public string? BucketType { get; set; }

	public required decimal BucketAmount { get; set; }

	public Guid? AnchorBucketId { get; set; }
}
