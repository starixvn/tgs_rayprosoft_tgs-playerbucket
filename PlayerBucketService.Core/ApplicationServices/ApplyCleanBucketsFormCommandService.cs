using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class ApplyCleanBucketsFormCommandService : IGetService<ApplyCleanBucketsFormCommand, Guid>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly ISerialNumberGenerator _serialNumberGenerator;

	public ApplyCleanBucketsFormCommandService(
		ITgsRepository tgsRepository,
		ISerialNumberGenerator serialNumberGenerator)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public async ValueTask<Guid> GetAsync(ApplyCleanBucketsFormCommand command, CancellationToken cancellationToken = default)
	{
		var createId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var form = new CleanPlayerBucketForm(
			createId,
			command.PlayerCode,
			command.BrandId,
			command.WalletId,
			command.CurrentBalance,
			command.CurrentBucketAmount,
			command.BucketAnchorId,
			command.DepositFormId,
			command.ClearWageringRequirementBalance,
			null,
			null,
			null,
			CleanFlowStatus.Create,
			DateTime.UtcNow);

		await _tgsRepository.UpsertCleanPlayerBucketFormAsync(
			form,
			cancellationToken).ConfigureAwait(false);

		return createId;
	}
}
