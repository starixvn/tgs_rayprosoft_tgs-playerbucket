using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class GetCleanBucketsFormListQueryService(ITgsRepository tgsRepository)
		: IQueryService<GetCleanBucketsFormListQuery, CleanBucketForm>
	, IQueryService<GetCleanBucketsFormListV2Query, CleanBucketForm>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public IAsyncEnumerable<CleanBucketForm> QueryAsync(GetCleanBucketsFormListQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryCleanPlayerBucketFormsV2Async(
			query.PlayerCode,
			query.FlowStatus,
			cancellationToken)
			.Select(form => new CleanBucketForm(
				form.Id,
				form.PlayerCode,
				form.WalletId,
				form.BrandId,
				form.CurrentBalance,
				form.CurrentBucketAmounts,
				form.DepositFormId,
				form.ClearWageringRequirementBalance,
				form.CleaningBalance,
				form.CleaningBucketAmounts,
				form.Status,
				form.CreatedOn));

	public IAsyncEnumerable<CleanBucketForm> QueryAsync(GetCleanBucketsFormListV2Query query, CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryCleanPlayerBucketFormsV2Async(
			query.PlayerCode,
			query.FlowStatus,
			cancellationToken)
			.Select(form => new CleanBucketForm(
				form.Id,
				form.PlayerCode,
				form.WalletId,
				form.BrandId,
				form.CurrentBalance,
				form.CurrentBucketAmounts,
				form.DepositFormId,
				form.ClearWageringRequirementBalance,
				form.CleaningBalance,
				form.CleaningBucketAmounts,
				form.Status,
				form.CreatedOn));
}
