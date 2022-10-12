using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Service.PlayerBuckets.ApplicationServices;

internal class FillInBucketCommandService : ICommandService<FillInBucketCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly ISerialNumberGenerator _serialNumberGenerator;

	public FillInBucketCommandService(ITgsRepository tgsRepository, ISerialNumberGenerator serialNumberGenerator)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public async ValueTask ExecuteAsync(FillInBucketCommand command, CancellationToken cancellationToken = default)
	{
		var allNotFullBuckets = _tgsRepository.GetPlayerNotFullBucketsAsync(command.PlayerCode, cancellationToken);

		var remainingAmount = command.Amount;

		await foreach (var bucket in allNotFullBuckets
			.WithCancellation(cancellationToken)
			.ConfigureAwait(false))
		{
			var bucketNotFillAmount = bucket.Amount - bucket.FillInAmount;

			var fillInfo = bucketNotFillAmount < remainingAmount
				? new FillInBucketInfo(
					bucketNotFillAmount,
					command.Type,
					command.ExternalId,
					true)
				: new FillInBucketInfo(
					remainingAmount,
					command.Type,
					command.ExternalId,
					bucketNotFillAmount == remainingAmount);

			remainingAmount -= fillInfo.Amount;

			await _tgsRepository.FillInBucketAsync(
				await _serialNumberGenerator
					.GenerateAsync(cancellationToken)
					.ConfigureAwait(false),
				bucket.Id,
				fillInfo,
				cancellationToken).ConfigureAwait(false);

			if (remainingAmount <= 0M)
				break;
		}
	}
}
