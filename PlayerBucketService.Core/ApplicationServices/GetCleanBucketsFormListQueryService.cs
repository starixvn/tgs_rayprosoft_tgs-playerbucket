using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class GetCleanBucketsFormListQueryService : IQueryService<GetCleanBucketsFormListQuery, CleanBucketForm>
{
	private readonly ITgsRepository _tgsRepository;

	public GetCleanBucketsFormListQueryService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public IAsyncEnumerable<CleanBucketForm> QueryAsync(GetCleanBucketsFormListQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryCleanPlayerBucketFormsAsync(
			query.PlayerCode,
			query.FlowStatus,
			cancellationToken)
			.Select(form => new CleanBucketForm(
				form.Id,
				form.PlayerCode,
				form.WalletId,
				form.BrandId,
				form.CurrentBalance,
				new[] {
					new KeyValuePair<string?, ReadBucketResult>(
						null,
						new ReadBucketResult(form.CurrentBucketAmount, form.BucketAnchorId))
				},
				form.DepositFormId,
				form.ClearWageringRequirementBalance,
				form.CleaningBalance,
				form.CleaningBucketAmount is null
					? null
					: new[] {
						new KeyValuePair<string?, ReadBucketResult>(
							null,
							new ReadBucketResult(form.CleaningBucketAmount!.Value, null))
					},
				form.Status,
				form.CreatedOn));
}
