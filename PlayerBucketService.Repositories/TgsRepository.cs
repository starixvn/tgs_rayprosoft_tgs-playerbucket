using System.Text.Json;
using Tgs.PlayerBuckets.DatabaseAccess;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;
using Tgs.SerialNumberGenerator;

namespace Tgs.Service.PlayerBuckets.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly PlayerBucketDataAccess _playerBucketDataAccess;
	private readonly ISerialNumberGenerator _serialNumberGenerator;

	public TgsRepository(PlayerBucketDataAccess playerBucketDataAccess, ISerialNumberGenerator serialNumberGenerator)
	{
		_playerBucketDataAccess = playerBucketDataAccess ?? throw new ArgumentNullException(nameof(playerBucketDataAccess));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public IAsyncEnumerable<BucketInfo> QueryPlayerBucketsAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var queryAfterDate = new DateTimeOffset(
			DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8D)).Date.AddDays(-30D),
			TimeSpan.FromHours(8D)).UtcDateTime;

		var notFullBuckets = _playerBucketDataAccess
			.GetPlayerNotFullBucketsAsync(playerCode, cancellationToken)
			.Select(row => new BucketInfo(
				row.Id,
				row.PlayerCode,
				row.BucketAmount,
				(BucketType)row.BucketType,
				row.CurrentBucketAmount,
				row.IsFull,
				row.CreatedOn,
				row.SnapshotId));
		var fullBuckets = _playerBucketDataAccess
			.GetPlayerFullBucketsAsync(playerCode, queryAfterDate, cancellationToken)
			.Select(row => new BucketInfo(
				row.Id,
				row.PlayerCode,
				row.BucketAmount,
				(BucketType)row.BucketType,
				row.CurrentBucketAmount,
				row.IsFull,
				row.CreatedOn,
				row.SnapshotId));

		return notFullBuckets.Concat(fullBuckets)
			.OrderByDescending(info => info.CreatedOn);
	}

	public IAsyncEnumerable<BucketEvent> QueryPlayerBucketHistoriesAsync(Guid bucketId, CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.GetBucketAllEventsAsync(bucketId, cancellationToken)
			.Take(1000)
			.Select(row => new BucketEvent(
				row.Id,
				row.BucketId,
				row.ChangeAmount,
				Enum.Parse<FillType>(row.FillType),
				row.ExternalId,
				JsonDocument.Parse(row.ChangeSets),
				row.CreatedOn,
				row.Remark));

	public IAsyncEnumerable<BucketEvent> QueryPlayerBucketHistoriesByIdAsync(
		Guid bucketId,
		Guid eventId,
		CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.GetBucketAllEventsAsync(bucketId, cancellationToken)
			.Where(row => row.Id.CompareTo(eventId) == 1)
			.Take(1000)
			.Select(row => new BucketEvent(
				row.Id,
				row.BucketId,
				row.ChangeAmount,
				Enum.Parse<FillType>(row.FillType),
				row.ExternalId,
				JsonDocument.Parse(row.ChangeSets),
				row.CreatedOn,
				row.Remark));

	public IAsyncEnumerable<BucketEvent> QueryPlayerBucketHistoriesByDateAsync(
		Guid bucketId,
		DateTime queryDate,
		CancellationToken cancellationToken = default)
	{
		var date = queryDate.ToUniversalTime();

		return _playerBucketDataAccess.GetBucketAllEventsAsync(bucketId, cancellationToken)
			.Where(row => row.CreatedOn > date)
			.Take(1000)
			.Select(row => new BucketEvent(
				row.Id,
				row.BucketId,
				row.ChangeAmount,
				Enum.Parse<FillType>(row.FillType),
				row.ExternalId,
				JsonDocument.Parse(row.ChangeSets),
				row.CreatedOn,
				row.Remark));
	}

	public async ValueTask UpsertCleanPlayerBucketFormAsync(
		CleanPlayerBucketForm form,
		CleanPlayerBucketForm? previousForm,
		CancellationToken cancellationToken = default)
	{
		var eventId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var eventSource = new PlayerBucketCleanEventSource
		{
			Id = eventId,
			FormId = form.Id,
			ChangeSets = JsonSerializer.Serialize(
				form.CreateJsonPatchDocument(previousForm)),
			CreatedOn = DateTime.UtcNow
		};

		await _playerBucketDataAccess.InsertPlayerBucketCleanEventSourceAsync(
			eventSource,
			cancellationToken).ConfigureAwait(false);

		var row = new PlayerBucketCleanForm
		{
			Id = form.Id,
			BrandId = form.BrandId,
			WalletId = form.WalletId,
			BucketAnchorId = form.BucketAnchorId,
			CleaningBalance = form.CleaningBalance,
			CleaningBucketAmount = form.CleaningBucketAmount,
			CreatedOn = form.CreatedOn,
			CurrentBalance = form.CurrentBalance,
			CurrentBucketAmount = form.CurrentBucketAmount,
			DepositFormId = form.DepositFormId,
			PlayerCode = form.PlayerCode,
			ClearWageringRequirementBalance = form.ClearWageringRequirementBalance,
			GameBalanceReturn = form.GameBalanceReturn is null
				? null
				: JsonSerializer.Serialize(form.GameBalanceReturn),
			Status = form.Status.ToString(),
			SnapshotId = eventId
		};

		await _playerBucketDataAccess.UpsertPlayerBucketCleanFormAsync(
			row,
			cancellationToken).ConfigureAwait(false);

		await _playerBucketDataAccess
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<CleanPlayerBucketForm?> GetCleanPlayerBucketFormAsync(
		Guid formId,
		CancellationToken cancellationToken = default)
	{
		var row = await _playerBucketDataAccess.FindPlayerBucketCleanFormAsync(
			formId,
			cancellationToken).ConfigureAwait(false);

		return row is null
			? null
			: new CleanPlayerBucketForm(
				row.Id,
				row.PlayerCode,
				row.BrandId,
				row.WalletId,
				row.CurrentBalance,
				row.CurrentBucketAmount,
				row.BucketAnchorId,
				row.DepositFormId,
				row.ClearWageringRequirementBalance,
				row.CleaningBalance,
				row.CleaningBucketAmount,
				string.IsNullOrWhiteSpace(row.GameBalanceReturn)
					? null
					: JsonDocument.Parse(row.GameBalanceReturn),
				Enum.Parse<CleanFlowStatus>(row.Status),
				row.CreatedOn);
	}

	public IAsyncEnumerable<CleanPlayerBucketForm> QueryCleanPlayerBucketFormsAsync(
		string playerCode,
		CleanFlowStatus? flowStatus,
		CancellationToken cancellationToken = default)
		=> _playerBucketDataAccess.QueryPlayerBucketCleanFormsAsync(
			playerCode,
			flowStatus?.ToString(),
			cancellationToken)
			.Select(row => new CleanPlayerBucketForm(
				row.Id,
				row.PlayerCode,
				row.BrandId,
				row.WalletId,
				row.CurrentBalance,
				row.CurrentBucketAmount,
				row.BucketAnchorId,
				row.DepositFormId,
				row.ClearWageringRequirementBalance,
				row.CleaningBalance,
				row.CleaningBucketAmount,
				string.IsNullOrWhiteSpace(row.GameBalanceReturn)
					? null
					: JsonDocument.Parse(row.GameBalanceReturn),
				Enum.Parse<CleanFlowStatus>(row.Status),
				row.CreatedOn));
}
