using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class CompleteCleanBucketsFormCommandService : ICommandService<CompleteCleanBucketsFormCommand>
{
	private readonly ITgsRepository _tgsRepository;

	public CompleteCleanBucketsFormCommandService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public async ValueTask ExecuteAsync(CompleteCleanBucketsFormCommand command, CancellationToken cancellationToken = default)
	{
		var form = await _tgsRepository.GetCleanPlayerBucketFormV2Async(
			command.FormId,
			cancellationToken).ConfigureAwait(false);

		if (form is null)
			return;

		var updateForm = form with
		{
			Status = CleanFlowStatus.Completed,
			CleaningBalance = command.CleaningBalance,
			CleaningBucketAmounts = command.CleaningBucketAmounts,
			GameBalanceReturn = command.GameBalanceReturn
		};

		await _tgsRepository.UpsertCleanPlayerBucketFormAsync(
			updateForm,
			form,
			cancellationToken).ConfigureAwait(false);
	}
}
