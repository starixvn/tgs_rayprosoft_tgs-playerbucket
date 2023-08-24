namespace PlayerBucketService.ViewModels;

public class CleanBucketsOrderV2ViewModel
{
	public required Guid BrandId { get; set; }

	public required Guid WalletId { get; set; }

	public required decimal CurrentBalance { get; set; }

	public required IEnumerable<CleanBucketResultViewModel> CurrentBucketAmounts { get; set; }

	public required Guid DepositFormId { get; set; }

	public required decimal ClearWageringRequirementBalance { get; set; }
}
