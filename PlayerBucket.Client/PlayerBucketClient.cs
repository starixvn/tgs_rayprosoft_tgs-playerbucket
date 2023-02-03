using System.Net.Http.Json;
using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucket.Clients;

internal class PlayerBucketClient : IPlayerBucketClient
{
	private readonly HttpClient _httpClient;
	private readonly IMessageSender _messageSender;

	public PlayerBucketClient(HttpClient httpClient, IMessageSender messageSender)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask<Guid> CreateBucketAsync(
		string playerCode,
		decimal amount,
		BucketType type,
		string? externalId,
		string? remark,
		CancellationToken cancellationToken = default)
	{
		var request = new CreateBucketRequest
		{
			PlayerCode = playerCode,
			Amount = amount,
			BucketType = type.ToString(),
			ExternalId = externalId ?? string.Empty,
			Remark = remark ?? string.Empty,
		};

		var result = await _messageSender
			.RequestAsync(
				"tgs.bucket.create",
				request.ToByteArray(),
				cancellationToken)
			.AsTask()
			.WaitAsync(TimeSpan.FromSeconds(3D), cancellationToken)
			.ConfigureAwait(false);

		var response = CreateBucketResponse.Parser.ParseFrom(result.Span);

		return response.BucketId.ToGuid();
	}

	public async ValueTask FillInBucketAsync(
		string playerCode,
		decimal amount,
		FillType type,
		string? externalId,
		string? remark,
		CancellationToken cancellationToken = default)
	{
		var request = new FillInRequest
		{
			PlayerCode = playerCode,
			Amount = amount,
			FillType = type.ToString(),
			ExternalId = externalId ?? string.Empty,
			Remark = remark ?? string.Empty
		};

		await _messageSender
			.SendAsync(
				"tgs.bucket.fill",
				request.ToByteArray(),
				cancellationToken)
			.AsTask()
			.WaitAsync(TimeSpan.FromSeconds(3D), cancellationToken)
			.ConfigureAwait(false);
	}

	public ValueTask SnapshotBucketAsync(string playerCode, Guid bucketId, CancellationToken cancellationToken = default)
	{
		var request = new SnapshotRequest
		{
			BucketId = bucketId,
			PlayerCode = playerCode
		};

		return _messageSender.PublishAsync("tgs.bucket.snapshot", request.ToByteArray(), cancellationToken);
	}

	public async ValueTask<ReadBucketResult> ReadBucketsAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var request = new GetBucketAmountRequest
		{
			PlayerCode = playerCode
		};

		var result = await _messageSender
			.RequestAsync(
				"tgs.bucket.amount",
				request.ToByteArray(),
				cancellationToken)
			.AsTask()
			.WaitAsync(TimeSpan.FromSeconds(3D), cancellationToken)
			.ConfigureAwait(false);

		var response = GetBucketAmountResponse.Parser.ParseFrom(result.Span);

		return new ReadBucketResult(response.Amount, (Guid?)response.AnchorBucketId);
	}

	public async ValueTask CleanBucketsAsync(
		string playerCode,
		Guid anchorBucketId,
		string? externalId,
		string? remark,
		CancellationToken cancellationToken = default)
	{
		var request = new CleanBucketRequest
		{
			PlayerCode = playerCode,
			AnchorBucketId = anchorBucketId,
			ExternalId = externalId ?? string.Empty,
			Remark = remark ?? string.Empty
		};

		await _messageSender
			.SendAsync(
				"tgs.bucket.clean",
				request.ToByteArray(),
				cancellationToken)
			.AsTask()
			.WaitAsync(TimeSpan.FromSeconds(3D), cancellationToken)
			.ConfigureAwait(false);
	}

	public IAsyncEnumerable<BucketInfo> GetBucketsByPlayerCodeAsync(string playerCode, CancellationToken cancellationToken = default)
		=> _httpClient.GetAsync($"/api/bucket/{playerCode}", cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<IEnumerable<BucketInfo>>(PlayerBucketUtility.SerializerOptions, cancellationToken)
				.ToAsyncEnumerable()
				.SelectMany(arr => arr!.ToAsyncEnumerable()))
			.Where(info => info is not null)
			.Select(info => info!)
			.ToEnumerable()
			.ToAsyncEnumerable();

	public IAsyncEnumerable<BucketEvent> GetBucketEventsAsync(Guid bucketId, CancellationToken cancellationToken = default)
		=> _httpClient.GetAsync($"api/bucket/history/{bucketId}", cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<IEnumerable<BucketEvent>>(PlayerBucketUtility.SerializerOptions, cancellationToken)
				.ToAsyncEnumerable()
				.SelectMany(arr => arr!.ToAsyncEnumerable()))
			.ToEnumerable()
			.ToAsyncEnumerable();

	public IAsyncEnumerable<BucketEvent> GetBucketEventsAsync(Guid bucketId, Guid eventId, CancellationToken cancellationToken = default)
		=> _httpClient.GetAsync($"api/bucket/history/{bucketId}?id={eventId}", cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<IEnumerable<BucketEvent>>(PlayerBucketUtility.SerializerOptions, cancellationToken)
				.ToAsyncEnumerable()
				.SelectMany(arr => arr!.ToAsyncEnumerable()))
			.ToEnumerable()
			.ToAsyncEnumerable();

	public IAsyncEnumerable<BucketEvent> GetBucketEventsAsync(Guid bucketId, DateTime queryDate, CancellationToken cancellationToken = default)
		=> _httpClient.GetAsync($"api/bucket/history/{bucketId}?date={queryDate:u}", cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<IEnumerable<BucketEvent>>(PlayerBucketUtility.SerializerOptions, cancellationToken)
				.ToAsyncEnumerable()
				.SelectMany(arr => arr!.ToAsyncEnumerable()))
			.ToEnumerable()
			.ToAsyncEnumerable();
}
