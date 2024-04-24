using System.Data;
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
	[HttpGet("{playerCode}/{flowStatus}", Name = "GetCleanBucketsFormListByFlowStatus")]
	[HttpGet("{playerCode}", Name = "GetCleanBucketsFormList")]
	[MapToApiVersion("1.0")]
	public IAsyncEnumerable<CleanBucketsFormViewModel> GetCleanBucketsFormListV1Async(
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

	[HttpGet("{playerCode}/{flowStatus}", Name = "GetCleanBucketsFormListByFlowStatus")]
	[HttpGet("{playerCode}", Name = "GetCleanBucketsFormList")]
	[MapToApiVersion("2.0")]
	public IAsyncEnumerable<CleanBucketsFormV2ViewModel> GetCleanBucketsFormListAsync(
		[FromServices] IQueryService<GetCleanBucketsFormListV2Query, CleanBucketForm> queryService,
		string playerCode,
		string? flowStatus)
	{
		var query = new GetCleanBucketsFormListV2Query(
			playerCode,
			string.IsNullOrWhiteSpace(flowStatus) ? null : Enum.Parse<CleanFlowStatus>(flowStatus));

		return queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(form => new CleanBucketsFormV2ViewModel
			{
				Id = form.Id,
				PlayerCode = form.PlayerCode,
				BrandId = form.BrandId,
				WalletId = form.WalletId,
				CurrentBalance = form.CurrentBalance,
				CurrentBucketAmounts = form.CurrentBucketAmounts
					.Select(result => new CleanBucketResultViewModel
					{
						BucketType = result.BucketType,
						ReadBucketResult = new ReadBucketResultViewModel
						{
							BucketAmount = result.Result.Amount,
							AnchorBucketId = result.Result.AnchorBucketId
						}
					}),
				DepositFormId = form.DepositFormId,
				ClearWageringRequirementBalance = form.ClearWageringRequirementBalance,
				CleaningBalance = form.CleaningBalance,
				CleaningBucketAmounts = form.CleaningBucketAmounts
					?.Select(result => new CleanBucketResultViewModel
					{
						BucketType = result.BucketType,
						ReadBucketResult = new ReadBucketResultViewModel
						{
							BucketAmount = result.Result.Amount,
							AnchorBucketId = result.Result.AnchorBucketId
						}
					}),
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
	[HttpGet("{formId:Guid}", Name = "GetCleanBucketsForm")]
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

	[MapToApiVersion("2.0")]
	[HttpGet("{formId:Guid}", Name = "GetCleanBucketsForm")]
	public async Task<CleanBucketsFormV2ViewModel?> GetCleanBucketsFormAsync(
		[FromServices] IGetService<GetCleanPlayerBucketsFormV2Query, CleanPlayerBucketForm?> getService,
		Guid formId)
	{
		var query = new GetCleanPlayerBucketsFormV2Query(formId);

		var form = await getService.GetAsync(
			query,
			HttpContext.RequestAborted).ConfigureAwait(false);

		return form is null
			? null
			: new CleanBucketsFormV2ViewModel
			{
				Id = form.Id,
				PlayerCode = form.PlayerCode,
				BrandId = form.BrandId,
				WalletId = form.WalletId,
				CurrentBalance = form.CurrentBalance,
				CurrentBucketAmounts = form.CurrentBucketAmounts
					.Select(result => new CleanBucketResultViewModel
					{
						BucketType = result.BucketType,
						ReadBucketResult = new ReadBucketResultViewModel
						{
							BucketAmount = result.Result.Amount,
							AnchorBucketId = result.Result.AnchorBucketId
						}
					}),
				DepositFormId = form.DepositFormId,
				ClearWageringRequirementBalance = form.ClearWageringRequirementBalance,
				CleaningBalance = form.CleaningBalance,
				CleaningBucketAmounts = form.CleaningBucketAmounts
					?.Select(result => new CleanBucketResultViewModel
					{
						BucketType = result.BucketType,
						ReadBucketResult = new ReadBucketResultViewModel
						{
							BucketAmount = result.Result.Amount,
							AnchorBucketId = result.Result.AnchorBucketId
						}
					}),
				Status = form.Status,
				CreatedOn = form.CreatedOn
			};
	}

	[HttpPut("{playerCode}", Name = "RequestCleanBuckets")]
	[SuccessStatusCode(201, "Created apply")]
	[MapToApiVersion("1.0")]
	public ValueTask<Guid> RequestCleanBucketsV1Async(
		[FromServices] IGetService<ApplyCleanBucketsFormCommand, Guid> applyService,
		string playerCode,
		[FromBody] CleanBucketsOrderViewModel orderModel)
	{
		var command = new ApplyCleanBucketsFormCommand(
			playerCode,
			orderModel.BrandId,
			orderModel.WalletId,
			orderModel.CurrentBalance,
			[
				new TypeBucketResult(
					null,
					new ReadBucketResult(
						orderModel.CurrentBucketAmount,
						orderModel.BucketAnchorId))
			],
			orderModel.DepositFormId,
			orderModel.ClearWageringRequirementBalance);

		return applyService.GetAsync(command, HttpContext.RequestAborted);
	}

	[HttpPut("{playerCode}", Name = "RequestCleanBuckets")]
	[SuccessStatusCode(201, "Created apply")]
	[MapToApiVersion("2.0")]
	public ValueTask<Guid> RequestCleanBucketsAsync(
		[FromServices] IGetService<ApplyCleanBucketsFormCommand, Guid> applyService,
		string playerCode,
		[FromBody] CleanBucketsOrderV2ViewModel orderModel)
	{
		var command = new ApplyCleanBucketsFormCommand(
			playerCode,
			orderModel.BrandId,
			orderModel.WalletId,
			orderModel.CurrentBalance,
			orderModel.CurrentBucketAmounts
				.Select(item => new TypeBucketResult(
					item.BucketType,
					new ReadBucketResult(
						item.ReadBucketResult.BucketAmount,
						item.ReadBucketResult.AnchorBucketId))),
			orderModel.DepositFormId,
			orderModel.ClearWageringRequirementBalance);

		return applyService.GetAsync(command, HttpContext.RequestAborted);
	}

	[HttpPost("{formId:Guid}:complete", Name = "CompleteCleanBucketsForm")]
	[MapToApiVersion("1.0")]
	public ValueTask CompleteCleanBucketsFormV1Async(
		[FromServices] ICommandService<CompleteCleanBucketsFormCommand> completeService,
		Guid formId,
		[FromBody] ReviewCleanBucketsFormViewModel viewModel)
	{
		var command = new CompleteCleanBucketsFormCommand(
			formId,
			viewModel.CleaningBalance,
			[
				new TypeBucketResult(
					null,
					new ReadBucketResult(
						viewModel.CleaningBucketAmount,
						null))
			],
			viewModel.GameBalanceReturn);

		return completeService.ExecuteAsync(command, HttpContext.RequestAborted);
	}

	[HttpPost("{formId:Guid}:complete", Name = "CompleteCleanBucketsForm")]
	[MapToApiVersion("2.0")]
	public ValueTask CompleteCleanBucketsFormAsync(
		[FromServices] ICommandService<CompleteCleanBucketsFormCommand> completeService,
		Guid formId,
		[FromBody] ReviewCleanBucketsFormV2ViewModel viewModel)
	{
		var command = new CompleteCleanBucketsFormCommand(
			formId,
			viewModel.CleaningBalance,
			viewModel.CleaningBucketAmounts
				.Select(item => new TypeBucketResult(
					item.BucketType,
					new ReadBucketResult(
						item.ReadBucketResult.BucketAmount,
						item.ReadBucketResult.AnchorBucketId))),
			viewModel.GameBalanceReturn);

		return completeService.ExecuteAsync(command, HttpContext.RequestAborted);
	}

	[HttpPost("{formId:Guid}:reject", Name = "RejectCleanBucketsForm")]
	[MapToApiVersion("1.0")]
	public ValueTask RejectCleanBucketsFormV1Async(
		[FromServices] ICommandService<RejectCleanBucketsFormCommand> rejectService,
		Guid formId,
		[FromBody] ReviewCleanBucketsFormViewModel viewModel)
	{
		var command = new RejectCleanBucketsFormCommand(
			formId,
			viewModel.CleaningBalance,
			[
				new TypeBucketResult(
					null,
					new ReadBucketResult(
						viewModel.CleaningBucketAmount,
						null))
			],
			viewModel.GameBalanceReturn);

		return rejectService.ExecuteAsync(command, HttpContext.RequestAborted);
	}

	[HttpPost("{formId:Guid}:reject", Name = "RejectCleanBucketsForm")]
	[MapToApiVersion("2.0")]
	public ValueTask RejectCleanBucketsFormAsync(
		[FromServices] ICommandService<RejectCleanBucketsFormCommand> rejectService,
		Guid formId,
		[FromBody] ReviewCleanBucketsFormV2ViewModel viewModel)
	{
		var command = new RejectCleanBucketsFormCommand(
			formId,
			viewModel.CleaningBalance,
			viewModel.CleaningBucketAmounts
				.Select(item => new TypeBucketResult(
					item.BucketType,
					new ReadBucketResult(
						item.ReadBucketResult.BucketAmount,
						item.ReadBucketResult.AnchorBucketId))),
			viewModel.GameBalanceReturn);

		return rejectService.ExecuteAsync(command, HttpContext.RequestAborted);
	}
}
