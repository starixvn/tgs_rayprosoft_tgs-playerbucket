using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBucketWorker.ViewModels;

public class BucketFillInViewModel
{
	public string PlayerCode { get; init; } = default!;

	public decimal Amount { get; init; }

	public FillType FillType { get; init; }

	public string? ExternalId { get; init; }
}
