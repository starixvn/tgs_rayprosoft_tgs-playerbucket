using Microsoft.AspNetCore.Mvc;
using Tgs.PlayerBucket.Clients;
using Tgs.PlayerBucketService.ViewModels;
using Tgs.PlayerBucketWorker.ViewModels;

namespace Tgs.PlayerBucketService.Controllers;

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
}
