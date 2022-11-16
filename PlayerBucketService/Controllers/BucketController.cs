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
				CreatedOn = info.CreatedOn,
				PlayerCode = info.PlayerCode
			});
	}

	[HttpGet("history/{bucketId}")]
	public IAsyncEnumerable<PlayerBucketEventViewModel> GetBucketHistoriesAsync(
		[FromServices] IQueryService<GetPlayerBucketHistoriesQuery, BucketEvent> queryService,
		Guid bucketId)
	{
		var query = new GetPlayerBucketHistoriesQuery(bucketId);

		return queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(evt => new PlayerBucketEventViewModel
			{
				Id = evt.Id,
				BucketId = evt.BucketId,
				ChangeAmount = evt.ChangeAmount,
				Type = evt.Type,
				CreatedOn = evt.CreatedOn,
				ExternalId = evt.ExternalId
			});
	}
}
