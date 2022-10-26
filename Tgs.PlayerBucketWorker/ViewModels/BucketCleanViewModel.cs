namespace Tgs.PlayerBucketWorker.ViewModels;

public class BucketCleanViewModel
{
	public string PlayerCode { get; init; } = default!;

	public Guid AnchorId { get; init; }

	public string? ExternalId { get; init; }
}
