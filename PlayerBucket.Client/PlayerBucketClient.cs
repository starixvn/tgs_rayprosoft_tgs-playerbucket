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

	public async ValueTask RequestCleanBucketsAsync(string playerCode, CleanBucketOrder order, CancellationToken cancellationToken = default)
		=> await _httpClient.PutAsJsonAsync(
			$"api/bucket/clean-form/{playerCode}",
			order,
			PlayerBucketUtility.SerializerOptions,
			cancellationToken)
			.ToAsyncEnumerable()
			.Do(response =>
			{
				if (response.EnsureSuccessStatusCode().StatusCode != System.Net.HttpStatusCode.Created)
					throw new HttpRequestException("Status code unexpected for created.");
			})
			.SingleOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

	public async ValueTask<CleanBucketForm?> GetCleanBucketFormAsync(Guid formId, CancellationToken cancellationToken = default)
		=> await _httpClient.GetAsync($"api/bucket/clean-form/{formId}?api-version=2.0", cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.Where(response => response.StatusCode == System.Net.HttpStatusCode.OK)
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<CleanBucketForm>(PlayerBucketUtility.SerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.Where(form => form is not null)
			.Select(form => form!)
			.FirstOrDefaultAsync(cancellationToken)
			.ConfigureAwait(false);

	public async ValueTask CompleteCleanBucketsAsync<TGameBalanceReturn>(
		Guid formId,
		ReviewCleanBucketRequest<TGameBalanceReturn> request,
		CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync(
			$"api/bucket/clean-form/{formId}:complete",
			new
			{
				request.CleaningBalance,
				request.CleaningBucketAmount,
				request.GameBalanceReturn
			},
			PlayerBucketUtility.SerializerOptions,
			cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}

	public async ValueTask RejectCleanBucketsAsync<TGameBalanceReturn>(
		Guid formId,
		ReviewCleanBucketRequest<TGameBalanceReturn> request,
		CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync(
			$"api/bucket/clean-form/{formId}:reject",
			new
			{
				request.CleaningBalance,
				request.CleaningBucketAmount,
				request.GameBalanceReturn
			},
			PlayerBucketUtility.SerializerOptions,
			cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}

	public async ValueTask PublishCleanBucketsFormCreatedEventAsync(Guid formId, CancellationToken cancellationToken = default)
	{
		var request = new CleanFormCreatedEvent
		{
			FormId = formId
		};

		await _messageSender.PublishAsync(
			"tgs.bucket.clean-form.created",
			request.ToByteArray(),
			cancellationToken).ConfigureAwait(false);
	}

	public IAsyncEnumerable<CleanBucketForm> QueryCleanBucketFomsAsync(
		string playerCode,
		CleanFlowStatus? flowStatus = null,
		CancellationToken cancellationToken = default)
		=> _httpClient.GetAsync(
			flowStatus is null
				? $"/api/bucket/clean-form/{playerCode}"
				: $"/api/bucket/clean-form/{playerCode}/{flowStatus}",
			cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<CleanBucketForm[]>(PlayerBucketUtility.SerializerOptions, cancellationToken)
				.ToAsyncEnumerable()
				.Where(arr => arr is not null)
				.SelectMany(arr => arr!.ToAsyncEnumerable()))
			.ToArrayAsync(cancellationToken: cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());

	public async ValueTask SetBrandBucketTypesAsync(Guid brandId, IEnumerable<string> bucketTypes, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsJsonAsync(
			$"/api/brand/{brandId}",
			bucketTypes,
			PlayerBucketUtility.SerializerOptions,
			cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}

	public IAsyncEnumerable<string> GetBrandBucketTypesAsync(Guid brandId, CancellationToken cancellationToken = default)
		=> _httpClient.GetFromJsonAsync<IAsyncEnumerable<string>>(
			$"/api/brand/{brandId}",
			cancellationToken)
			.ToAsyncEnumerable()
			.Where(bucketTypes => bucketTypes is not null)
			.SelectMany(bucketTypes => bucketTypes!);
}
