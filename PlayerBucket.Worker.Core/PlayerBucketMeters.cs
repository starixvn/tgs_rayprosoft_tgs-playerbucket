using System.Diagnostics.Metrics;

namespace Tgs.Service.PlayerBuckets;

internal static class PlayerBucketMeters
{
	public static readonly UpDownCounter<int> ProcessPlayers;
	public static readonly ObservableGauge<long> TotalMemory;
	public static readonly ObservableGauge<long> UsedMemory;
	private static readonly Meter _PlayerBucketMeter = new("Tgs.PlayerBucket");

	static PlayerBucketMeters()
	{
		ProcessPlayers = _PlayerBucketMeter.CreateUpDownCounter<int>("players");
		TotalMemory = _PlayerBucketMeter.CreateObservableGauge("total-memory", () =>
		{
			var memoryInfo = GC.GetGCMemoryInfo();

			return new Measurement<long>(memoryInfo.TotalAvailableMemoryBytes);
		});
		UsedMemory = _PlayerBucketMeter.CreateObservableGauge("used-memory", () =>
		{
			var memoryInfo = GC.GetGCMemoryInfo();

			return new Measurement<long>(memoryInfo.TotalCommittedBytes);
		});
	}
}
