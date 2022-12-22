using System.Diagnostics.Metrics;

namespace Tgs.Service.PlayerBuckets;

public class PlayerBucketMetrics
{
	internal static readonly Histogram<int> ProcessPlayers;
	private static readonly Meter _PlayerBucketMeter = new("Tgs.PlayerBucket");
	private static readonly MeterListener _Listener;
	private static int _ProcessPlayerCount;

	static PlayerBucketMetrics()
	{
		ProcessPlayers = _PlayerBucketMeter.CreateHistogram<int>("processing.players.count");

		_ = _PlayerBucketMeter.CreateObservableGauge(
			"memory.available",
			() =>
			{
				var memoryInfo = GC.GetGCMemoryInfo();

				return new Measurement<long>(memoryInfo.TotalAvailableMemoryBytes);
			},
			"Bytes");

		_PlayerBucketMeter.CreateObservableGauge("processing.players", () => _ProcessPlayerCount, "numbers of people");

		_Listener = new MeterListener();

		_Listener.EnableMeasurementEvents(ProcessPlayers);

		_Listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, _) =>
		{
			_ProcessPlayerCount = measurement;
		});

		_Listener.Start();
	}
}
