using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tgs.PlayerBuckets.Committer;

public class OutOfMemoryExceptionHealthCheck : IHealthCheck
{
	private static bool _exceptionHappened = false;

	public static void SetExceptionHappened()
	{
		if (!_exceptionHappened)
			_exceptionHappened = true;
	}

	public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
		=> _exceptionHappened
			? Task.FromResult(HealthCheckResult.Unhealthy("OOM has occur."))
			: Task.FromResult(HealthCheckResult.Healthy());
}
