using Microsoft.AspNetCore.Mvc;
using Tgs.PlayerBucketWorker.ViewModels;
using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBucketWorker.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BucketController : ControllerBase
{
	[HttpPut(Name = "CreateBucket")]
	public ValueTask<Guid> CreateBucketAsync(
		[FromServices] IPlayerBucketClient playerBucketClient,
		BucketCreateViewModel viewModel)
		=> playerBucketClient.CreateBucketAsync(
			viewModel.PlayerCode,
			viewModel.Amount,
			viewModel.BucketType,
			viewModel.ExternalId,
			null,
			HttpContext.RequestAborted);

	[HttpDelete(Name = "FillInBucket")]
	public ValueTask FillInBucketAsync(
		[FromServices] IPlayerBucketClient playerBucketClient,
		BucketFillInViewModel viewModel)
		=> playerBucketClient.FillInBucketAsync(
			viewModel.PlayerCode,
			viewModel.Amount,
			viewModel.FillType,
			viewModel.ExternalId,
			HttpContext.RequestAborted);

	[HttpGet(Name = "GetBucketAmount")]
	public async ValueTask<BucketSummaryViewModel> GetBucketAmountAsync(
		[FromServices] IPlayerBucketClient playerBucketClient,
		string playerCode)
	{
		var result = await playerBucketClient.ReadBucketsAsync(
			playerCode,
			HttpContext.RequestAborted).ConfigureAwait(false);

		return new BucketSummaryViewModel
		{
			Amount = result.Amount,
			AnchorId = result.AnchorBucketId
		};
	}

	[HttpPut("snapshot/{playerCode}/{bucketId}", Name = "SnapshotBucket")]
	public ValueTask SnapshotBucketAsync(
		[FromServices] IPlayerBucketClient playerBucketClient,
		string playerCode,
		Guid bucketId)
		=> playerBucketClient.SnapshotBucketAsync(playerCode, bucketId, HttpContext.RequestAborted);

	[HttpPut("flush/{playerCode}/{bucketId}", Name = "FlushBucket")]
	public ValueTask FrashBucketAsync(
		[FromServices] IEventSource eventSource,
		string playerCode,
		Guid bucketId)
		=> eventSource.PublishBucketChangedAsync(
			new BucketChangedEvent(bucketId, playerCode),
			HttpContext.RequestAborted);

	[HttpDelete("clean", Name = "CleanBuckets")]
	public ValueTask CleanBucketsAsync(
		[FromServices] IPlayerBucketClient playerBucketClient,
		BucketCleanViewModel viewModel)
		=> playerBucketClient.CleanBucketsAsync(
			viewModel.PlayerCode,
			viewModel.AnchorId,
			viewModel.ExternalId,
			HttpContext.RequestAborted);
}
