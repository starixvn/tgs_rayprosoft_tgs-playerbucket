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
	public ValueTask<decimal> GetBucketAmountAsync(
		[FromServices] IPlayerBucketClient playerBucketClient,
		string playerCode)
		=> playerBucketClient.GetBucketAmountAsync(playerCode, HttpContext.RequestAborted);

	[HttpPut("snapshot/{playerCode}/{bucketId}", Name = "SnapshotBucket")]
	public ValueTask SnapshotBucketAsync(
		[FromServices] IPlayerBucketClient playerBucketClient,
		string playerCode,
		Guid bucketId)
		=> playerBucketClient.SnapshotBucketAsync(playerCode, bucketId, HttpContext.RequestAborted);
}
