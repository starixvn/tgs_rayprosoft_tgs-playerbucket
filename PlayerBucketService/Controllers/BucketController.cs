using Microsoft.AspNetCore.Mvc;
using PlayerBucketService.ViewModels;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;
using Tgs.ServiceUtility;
using Tgs.WebApi.Toolkits;

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

	[HttpPut("clean-form/{playerCode}", Name = "RequestCleanBuckets")]
	[SuccessStatusCode(201, "Created apply")]
	public ValueTask<Guid> RequestCleanBucketsAsync(
		[FromServices] IGetService<ApplyCleanBucketsFormCommand, Guid> applyService,
		string playerCode,
		[FromBody] CleanBucketsOrderViewModel orderModel)
	{
		var command = new ApplyCleanBucketsFormCommand(
			playerCode,
			orderModel.BrandId,
			orderModel.BucketAnchorId,
			orderModel.CurrentBalance,
			orderModel.CurrentBucketAmount,
			orderModel.DepositFormId);

		return applyService.GetAsync(command, HttpContext.RequestAborted);
	}

	/// <summary>
	/// Gets the clean buckets form asynchronous.
	/// </summary>
	/// <param name="getService">The get service.</param>
	/// <param name="formId">The form identifier.</param>
	/// <returns></returns>
	/// <response code="200">Find form</response>
	/// <response code="204">Not found</response>
	[HttpGet("clean-form/{formId}", Name = "GetCleanBucketsForm")]
	public async Task<CleanBucketsFormViewModel?> GetCleanBucketsFormAsync(
		[FromServices] IGetService<GetCleanPlayerBucketsFormQuery, CleanPlayerBucketForm?> getService,
		Guid formId)
	{
		var query = new GetCleanPlayerBucketsFormQuery(formId);

		var form = await getService.GetAsync(
			query,
			HttpContext.RequestAborted).ConfigureAwait(false);

		return form is null
			? null
			: new CleanBucketsFormViewModel
			{
				Id = form.Id,
				PlayerCode = form.PlayerCode,
				BrandId = form.BrandId,
				BucketAnchorId = form.BucketAnchorId,
				CurrentBalance = form.CurrentBalance,
				CurrentBucketAmount = form.CurrentBucketAmount,
				DepositFormId = form.DepositFormId,
				CleaningBalance = form.CleaningBalance,
				CleaningBucketAmount = form.CleaningBucketAmount,
				Status = form.Status,
				CreatedOn = form.CreatedOn
			};
	}

	[HttpPost("clean-form/{formId}", Name = "CompleteCleanBucketsForm")]
	public ValueTask CompleteCleanBucketsFormAsync(
		[FromServices] ICommandService<CompleteCleanBucketsFormCommand> completeService,
		Guid formId,
		[FromBody] CompleteCleanBucketsFormViewModel viewModel)
	{
		var command = new CompleteCleanBucketsFormCommand(
			formId,
			viewModel.CleaningBalance,
			viewModel.CleaningBucketAmount);

		return completeService.ExecuteAsync(command, HttpContext.RequestAborted);
	}
}
