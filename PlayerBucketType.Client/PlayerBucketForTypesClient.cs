using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBucket.Client;

internal class PlayerBucketForTypesClient : IPlayerBucketForTypesClient
{
	private readonly IPlayerBucketClient _playerBucketClient;

	public PlayerBucketForTypesClient(IPlayerBucketClient playerBucketClient)
	{
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
	}

	public async ValueTask CreateBucketForBucketTypesAsync(
		string playerCode,
		decimal amount,
		BucketType type,
		string? externalId,
		string? remark,
		IEnumerable<string>? bucketTypes = default,
		CancellationToken cancellationToken = default)
	{
		var bucketTypesArray = bucketTypes?.ToArray();
		var allTasks = new List<Task>((bucketTypesArray?.Length ?? 0) + 1)
		{
			_playerBucketClient.CreateBucketAsync(
				playerCode,
				amount,
				type,
				externalId,
				remark,
				cancellationToken).AsTask()
		};

		if (bucketTypesArray is not null)
			allTasks.AddRange(bucketTypesArray
				.Select(bucketType => _playerBucketClient.CreateBucketAsync(
					$"{playerCode}:{bucketType}",
					amount,
					type,
					externalId,
					remark,
					cancellationToken).AsTask()));

		await Task.WhenAll(allTasks).ConfigureAwait(false);
	}

	public async ValueTask FillInBucketForBucketTypesAsync(
		string playerCode,
		decimal amount,
		FillType type,
		string? externalId,
		string? remark,
		IEnumerable<string>? bucketTypes = default,
		CancellationToken cancellationToken = default)
	{
		var bucketTypesArray = bucketTypes?.ToArray();
		var allTasks = new List<Task>((bucketTypesArray?.Length ?? 0) + 1)
		{
			_playerBucketClient.FillInBucketAsync(
				playerCode,
				amount,
				type,
				externalId,
				remark,
				cancellationToken).AsTask()
		};

		if (bucketTypesArray is not null)
			allTasks.AddRange(bucketTypesArray
				.Select(bucketType => _playerBucketClient.FillInBucketAsync(
					$"{playerCode}:{bucketType}",
					amount,
					type,
					externalId,
					remark,
					cancellationToken).AsTask()));

		await Task.WhenAll(allTasks).ConfigureAwait(false);
	}

	public IAsyncEnumerable<(string BucketName, ReadBucketResult Result)> ReadBucketsForBucketTypesAsync(
		string playerCode,
		IEnumerable<string>? bucketTypes = default,
		CancellationToken cancellationToken = default)
	{
		var bucketTypesArray = bucketTypes?.ToArray() ?? Array.Empty<string>();

		return AsyncEnumerableEx.Merge(
			_playerBucketClient.ReadBucketsAsync(
				playerCode,
				cancellationToken)
				.AsTask()
				.ToAsyncEnumerable()
				.Select(result => (BucketName: playerCode, Result: result)),
			bucketTypesArray
				.ToAsyncEnumerable()
				.SelectMany(bucketType => AsyncEnumerableEx.Return($"{playerCode}:{bucketType}")
					.SelectMany(bucketName => _playerBucketClient
						.ReadBucketsAsync(bucketName, cancellationToken)
						.AsTask()
						.ToAsyncEnumerable()
						.Select(result => (BucketName: bucketName, Result: result)))));
	}

	public async ValueTask CleanBucketsForBucketTypesAsync(
		string playerCode,
		IEnumerable<(string? BucketType, Guid AnchorBucketId)> anchorBucketIdsByType,
		string? externalId,
		string? remark,
		CancellationToken cancellationToken = default)
		=> await Task.WhenAll(anchorBucketIdsByType
			.Select(kv => kv.BucketType is null
				? _playerBucketClient.CleanBucketsAsync(
					playerCode,
					kv.AnchorBucketId,
					externalId,
					remark,
					cancellationToken)
				: _playerBucketClient.CleanBucketsAsync(
					$"{playerCode}:{kv.BucketType}",
					kv.AnchorBucketId,
					externalId,
					remark,
					cancellationToken))
			.Select(vt => vt.AsTask())
			.ToArray()).ConfigureAwait(false);
}
