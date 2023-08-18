using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBucket.Clients;

internal class BucketCleanFormV1
{
	public Guid Id { get; set; }

	public string PlayerCode { get; set; } = default!;

	public Guid WalletId { get; set; }

	public Guid BrandId { get; set; }

	public Guid BucketAnchorId { get; set; }

	public decimal CurrentBalance { get; set; }

	public decimal CurrentBucketAmount { get; set; }

	public Guid DepositFormId { get; set; }

	public decimal ClearWageringRequirementBalance { get; set; }

	public decimal? CleaningBalance { get; set; }

	public decimal? CleaningBucketAmount { get; set; }

	public CleanFlowStatus Status { get; set; }

	public DateTime CreatedOn { get; set; }
}
