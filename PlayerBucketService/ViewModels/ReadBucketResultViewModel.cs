namespace PlayerBucketService.ViewModels;

public class ReadBucketResultViewModel
{
	public required decimal BucketAmount { get; set; }

	public Guid? AnchorBucketId { get; set; }
}
