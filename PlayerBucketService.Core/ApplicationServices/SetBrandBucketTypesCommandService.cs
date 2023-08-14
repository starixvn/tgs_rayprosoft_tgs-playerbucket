using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class SetBrandBucketTypesCommandService : ICommandService<SetBrandBucketTypesCommand>
{
	private readonly ITgsRepository _tgsRepository;

	public SetBrandBucketTypesCommandService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public ValueTask ExecuteAsync(SetBrandBucketTypesCommand command, CancellationToken cancellationToken = default)
		=> _tgsRepository.SetBrandBucketTypesAsync(
			command.BrandId,
			command.BucketTypes,
			cancellationToken);
}
