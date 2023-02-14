using Microsoft.AspNetCore.Mvc;
using PlayerBucketService.ViewModels;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;
using Tgs.ServiceUtility;
using Tgs.WebApi.Toolkits;

namespace PlayerBucketService.Controllers;

[Route("api/bucket/clean-form")]
[ApiController]
public class BucketCleanFormController : ControllerBase
{
	[HttpGet("{playerCode}/{flowStatus}", Name = "GetCleanBucketsFormListByFlowStatus")]
	[HttpGet("{playerCode}", Name = "GetCleanBucketsFormList")]
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
				BucketAnchorId = form.BucketAnchorId,
				CurrentBalance = form.CurrentBalance,
				CurrentBucketAmount = form.CurrentBucketAmount,
				DepositFormId = form.DepositFormId,
				ClearWageringRequirementBalance = form.ClearWageringRequirementBalance,
				CleaningBalance = form.CleaningBalance,
				CleaningBucketAmount = form.CleaningBucketAmount,
				Status = form.Status,
				CreatedOn = form.CreatedOn
			});
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

	/// <summary>
	/// Gets the clean buckets form asynchronous.
	/// </summary>
	/// <param name="getService">The get service.</param>
	/// <param name="formId">The form identifier.</param>
	/// <returns></returns>
	/// <response code="200">Find form</response>
	/// <response code="204">Not found</response>
	[HttpGet("{formId:Guid}", Name = "GetCleanBucketsForm")]
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
				WalletId = form.WalletId,
				BucketAnchorId = form.BucketAnchorId,
				CurrentBalance = form.CurrentBalance,
				CurrentBucketAmount = form.CurrentBucketAmount,
				DepositFormId = form.DepositFormId,
				ClearWageringRequirementBalance = form.ClearWageringRequirementBalance,
				CleaningBalance = form.CleaningBalance,
				CleaningBucketAmount = form.CleaningBucketAmount,
				Status = form.Status,
				CreatedOn = form.CreatedOn
			};
	}

	[HttpPost("{formId}:complete", Name = "CompleteCleanBucketsForm")]
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

	[HttpPost("{formId}:reject", Name = "RejectCleanBucketsForm")]
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
