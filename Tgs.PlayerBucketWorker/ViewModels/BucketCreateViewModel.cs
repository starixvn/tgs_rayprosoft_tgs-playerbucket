using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBucketWorker.ViewModels;

public class BucketCreateViewModel
{
	public string PlayerCode { get; init; } = default!;

	public decimal Amount { get; init; }

	public BucketType BucketType { get; init; }

	public string? ExternalId { get; init; }
}
