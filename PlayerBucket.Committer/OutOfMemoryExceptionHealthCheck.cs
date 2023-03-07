using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tgs.PlayerBuckets.Committer;

public class OutOfMemoryExceptionHealthCheck : IHealthCheck
{
	private static bool _ExceptionHappened = false;

	public static void SetExceptionHappened()
	{
		if (!_ExceptionHappened)
			_ExceptionHappened = true;
	}

	public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
		=> _ExceptionHappened
			? Task.FromResult(HealthCheckResult.Unhealthy("OOM has occur."))
			: Task.FromResult(HealthCheckResult.Healthy());
}
