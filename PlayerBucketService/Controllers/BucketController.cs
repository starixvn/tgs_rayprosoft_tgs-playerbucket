using Microsoft.AspNetCore.Mvc;
using PlayerBucketService.ViewModels;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;
using Tgs.ServiceUtility;

namespace PlayerBucketService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BucketController : ControllerBase
{
	[HttpGet("{playerCode}", Name = "GetBucketsByPlayerCode")]
	public IAsyncEnumerable<PlayerBucketViewModel> GetBucketsByPlayerCodeAsync(
		[FromServices] IQueryService<GetPlayerBucketsQuery, BucketInfo> queryService,
		string playerCode)
	{
		var query = new GetPlayerBucketsQuery(playerCode);

		return queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(info => new PlayerBucketViewModel
			{
				BucketId = info.Id,
				Amount = info.Amount,
				BucketType = info.Type,
				FillInAmount = info.FillInAmount,
				CreatedOn = info.CreatedOn
			});
	}
}
