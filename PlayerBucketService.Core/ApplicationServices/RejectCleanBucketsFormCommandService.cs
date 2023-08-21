using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class RejectCleanBucketsFormCommandService : ICommandService<RejectCleanBucketsFormCommand>
{
	private readonly ITgsRepository _tgsRepository;

	public RejectCleanBucketsFormCommandService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public async ValueTask ExecuteAsync(RejectCleanBucketsFormCommand command, CancellationToken cancellationToken = default)
	{
		var form = await _tgsRepository.GetCleanPlayerBucketFormAsync(
			command.FormId,
			cancellationToken).ConfigureAwait(false);

		if (form is null)
			return;

		var updateForm = form with
		{
			Status = CleanFlowStatus.Rejected,
			CleaningBalance = command.CleaningBalance,
			CleaningBucketAmounts = new[]{
				new CleanFormBucketResult(
					null,
					new ReadBucketResult(command.CleaningBucketAmount, null))
			},
			GameBalanceReturn = command.GameBalanceReturn
		};

		await _tgsRepository.UpsertCleanPlayerBucketFormAsync(
			updateForm,
			form,
			cancellationToken).ConfigureAwait(false);
	}
}
