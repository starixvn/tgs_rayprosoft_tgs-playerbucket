using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PlayerBucketService.ViewModels;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;
using Tgs.ServiceUtility;

namespace PlayerBucketService.Controllers;

[ApiVersionNeutral]
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

	[HttpGet("history/{bucketId}", Name = "GetBucketHistories")]
	public IAsyncEnumerable<PlayerBucketEventViewModel> GetBucketHistoriesAsync(
		[FromServices] IServiceCoordinator serviceCoordinator,
		Guid bucketId,
		[FromQuery] PlayerBucketEventQueryViewModel queryModel)
	{
		return (queryModel switch
		{
			{ EventId: not null } => serviceCoordinator.QueryAsync<GetPlayerBucketHistoriesByIdQuery, BucketEvent>(
				new GetPlayerBucketHistoriesByIdQuery(bucketId, queryModel.EventId!.Value),
				HttpContext.RequestAborted),
			{ QueryDate: not null } => serviceCoordinator.QueryAsync<GetPlayerBucketHistoriesByDateQuery, BucketEvent>(
				new GetPlayerBucketHistoriesByDateQuery(bucketId, queryModel.QueryDate!.Value),
				HttpContext.RequestAborted),
			_ => serviceCoordinator.QueryAsync<GetPlayerBucketHistoriesQuery, BucketEvent>(
				new GetPlayerBucketHistoriesQuery(bucketId),
				HttpContext.RequestAborted)
		}).Select(evt => new PlayerBucketEventViewModel
		{
			Id = evt.Id,
			BucketId = evt.BucketId,
			ChangeAmount = evt.ChangeAmount,
			Type = evt.Type,
			CreatedOn = evt.CreatedOn,
			ExternalId = evt.ExternalId,
			Remark = evt.Remark
		});
	}
}
