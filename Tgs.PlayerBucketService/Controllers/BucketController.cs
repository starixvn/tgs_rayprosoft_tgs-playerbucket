using Microsoft.AspNetCore.Mvc;
using Tgs.PlayerBucketService.ViewModels;
using Tgs.Service.PlayerBuckets.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.PlayerBucketService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BucketController : ControllerBase
{
	[HttpPut(Name = "CreateBucket")]
	public ValueTask<Guid> CreateBucketAsync(
		[FromServices] IGetService<CreateBucketCommand, Guid> createService,
		BucketCreateViewModel viewModel)
	{
		var command = new CreateBucketCommand(
			viewModel.PlayerCode,
			viewModel.Amount,
			viewModel.BucketType);

		return createService.GetAsync(command, HttpContext.RequestAborted);
	}
}
