using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PlayerBucketService.ViewModels;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;
using Tgs.ServiceUtility;

namespace PlayerBucketService.Controllers;

[Route("api/bucket/clean-form")]
[ApiController]
[ControllerName("BucketCleanForm")]
public class BucketCleanFormV2Controller : ControllerBase
{
	[ApiVersion("2.0")]
	[HttpGet("{formId:Guid}", Name = "GetCleanBucketsForm")]
	public async Task<CleanBucketsFormV2ViewModel?> GetCleanBucketsFormAsync(
		[FromServices] IGetService<GetCleanPlayerBucketsFormQuery, CleanPlayerBucketForm?> getService,
		Guid formId)
	{
		var query = new GetCleanPlayerBucketsFormQuery(formId);

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
							AnchorBucketId = result.Result.AnchorBucketId!.Value
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
							AnchorBucketId = result.Result.AnchorBucketId!.Value
						}
					}),
				Status = form.Status,
				CreatedOn = form.CreatedOn
			};
	}
}
