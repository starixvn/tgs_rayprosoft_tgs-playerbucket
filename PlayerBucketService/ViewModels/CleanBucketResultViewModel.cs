namespace PlayerBucketService.ViewModels;

public class CleanBucketResultViewModel
{
	public string? BucketType { get; set; }

	public required ReadBucketResultViewModel ReadBucketResult { get; set; }
}
