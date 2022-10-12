using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class CreateBucketCommandService : IGetService<CreateBucketCommand, Guid>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly ISerialNumberGenerator _serialNumberGenerator;

	public CreateBucketCommandService(ITgsRepository tgsRepository, ISerialNumberGenerator serialNumberGenerator)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public async ValueTask<Guid> GetAsync(CreateBucketCommand command, CancellationToken cancellationToken = default)
	{
		var createId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);
		var bucketId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		await _tgsRepository.AddPlayerBucketAsync(
			createId,
			bucketId,
			new BucketCreateInfo(
				command.PlayerCode,
				command.Amount,
				command.BucketType),
			cancellationToken).ConfigureAwait(false);

		return createId;
	}
}
