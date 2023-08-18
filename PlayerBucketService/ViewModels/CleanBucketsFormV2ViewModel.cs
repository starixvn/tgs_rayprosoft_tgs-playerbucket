using Tgs.Service.PlayerBuckets;

namespace PlayerBucketService.ViewModels;

public class CleanBucketsFormV2ViewModel
{
	public required Guid Id { get; set; }

	public required string PlayerCode { get; set; }

	public required Guid WalletId { get; set; }

	public required Guid BrandId { get; set; }

	public required decimal CurrentBalance { get; set; }

	public required decimal CurrentBucketAmount { get; set; }

	public required IEnumerable<ReadBucketResultViewModel> CurrentBucketAmounts { get; set; }

	public required Guid DepositFormId { get; set; }

	public required decimal ClearWageringRequirementBalance { get; set; }

	public decimal? CleaningBalance { get; set; }

	public IEnumerable<ReadBucketResultViewModel>? CleaningBucketAmounts { get; set; }

	public required CleanFlowStatus Status { get; set; }

	public required DateTime CreatedOn { get; set; }
}
