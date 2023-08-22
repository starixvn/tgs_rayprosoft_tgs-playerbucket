using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PlayerBucketService.ViewModels;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;
using Tgs.ServiceUtility;
using Tgs.WebApi.Toolkits;

namespace PlayerBucketService.Controllers;

[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/bucket/clean-form")]
[ApiController]
[ControllerName("BucketCleanForm")]
public class BucketCleanFormController : ControllerBase
{
	[HttpGet("{playerCode}/{flowStatus}", Name = "GetCleanBucketsFormListByFlowStatus", Order = 1001)]
	[HttpGet("{playerCode}", Name = "GetCleanBucketsFormList", Order = 1000)]
	public IAsyncEnumerable<CleanBucketsFormViewModel> GetCleanBucketsFormListAsync(
		[FromServices] IQueryService<GetCleanBucketsFormListQuery, CleanBucketForm> queryService,
		string playerCode,
		string? flowStatus)
	{
		var query = new GetCleanBucketsFormListQuery(
			playerCode,
			string.IsNullOrWhiteSpace(flowStatus) ? null : Enum.Parse<CleanFlowStatus>(flowStatus));

		return queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(form => new CleanBucketsFormViewModel
			{
				Id = form.Id,
				PlayerCode = form.PlayerCode,
				BrandId = form.BrandId,
				WalletId = form.WalletId,
				BucketAnchorId = form.CurrentBucketAmounts.First().Result.AnchorBucketId!.Value,
				CurrentBalance = form.CurrentBalance,
				CurrentBucketAmount = form.CurrentBucketAmounts.First().Result.Amount,
				DepositFormId = form.DepositFormId,
				ClearWageringRequirementBalance = form.ClearWageringRequirementBalance,
				CleaningBalance = form.CleaningBalance,
				CleaningBucketAmount = form.CleaningBucketAmounts?.First().Result.Amount,
				Status = form.Status,
				CreatedOn = form.CreatedOn
			});
	}

	/// <summary>
	/// Gets the clean buckets form asynchronous.
	/// </summary>
	/// <param name="getService">The get service.</param>
	/// <param name="formId">The form identifier.</param>
	/// <returns></returns>
	/// <response code="200">Find form</response>
	/// <response code="204">Not found</response>
	[MapToApiVersion("1.0")]
	[HttpGet("{formId:Guid}", Name = "GetCleanBucketsForm", Order = 10)]
	public async Task<CleanBucketsFormViewModel?> GetCleanBucketsFormV1Async(
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
				WalletId = form.WalletId,
				BucketAnchorId = form.CurrentBucketAmounts
					.First(item => item.BucketType is null)
					.Result
					.AnchorBucketId
					!.Value,
				CurrentBalance = form.CurrentBalance,
				CurrentBucketAmount = form.CurrentBucketAmounts
					.First(item => item.BucketType is null)
					.Result
					.Amount,
				DepositFormId = form.DepositFormId,
				ClearWageringRequirementBalance = form.ClearWageringRequirementBalance,
				CleaningBalance = form.CleaningBalance,
				CleaningBucketAmount = form.CleaningBucketAmounts
					?.FirstOrDefault(item => item.BucketType is null)
					?.Result
					.Amount,
				Status = form.Status,
				CreatedOn = form.CreatedOn
			};
	}

	[HttpPut("{playerCode}", Name = "RequestCleanBuckets")]
	[SuccessStatusCode(201, "Created apply")]
	public ValueTask<Guid> RequestCleanBucketsAsync(
		[FromServices] IGetService<ApplyCleanBucketsFormCommand, Guid> applyService,
		string playerCode,
		[FromBody] CleanBucketsOrderViewModel orderModel)
	{
		var command = new ApplyCleanBucketsFormCommand(
			playerCode,
			orderModel.BrandId,
			orderModel.WalletId,
			orderModel.BucketAnchorId,
			orderModel.CurrentBalance,
			orderModel.CurrentBucketAmount,
			orderModel.DepositFormId,
			orderModel.ClearWageringRequirementBalance);

		return applyService.GetAsync(command, HttpContext.RequestAborted);
	}

	[HttpPost("{formId:Guid}:complete", Name = "CompleteCleanBucketsForm")]
	public ValueTask CompleteCleanBucketsFormAsync(
		[FromServices] ICommandService<CompleteCleanBucketsFormCommand> completeService,
		Guid formId,
		[FromBody] ReviewCleanBucketsFormViewModel viewModel)
	{
		var command = new CompleteCleanBucketsFormCommand(
			formId,
			viewModel.CleaningBalance,
			viewModel.CleaningBucketAmount,
			viewModel.GameBalanceReturn);

		return completeService.ExecuteAsync(command, HttpContext.RequestAborted);
	}

	[HttpPost("{formId:Guid}:reject", Name = "RejectCleanBucketsForm")]
	public ValueTask RejectCleanBucketsFormAsync(
		[FromServices] ICommandService<RejectCleanBucketsFormCommand> rejectService,
		Guid formId,
		[FromBody] ReviewCleanBucketsFormViewModel viewModel)
	{
		var command = new RejectCleanBucketsFormCommand(
			formId,
			viewModel.CleaningBalance,
			viewModel.CleaningBucketAmount,
			viewModel.GameBalanceReturn);

		return rejectService.ExecuteAsync(command, HttpContext.RequestAborted);
	}
}
