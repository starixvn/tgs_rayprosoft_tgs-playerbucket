using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tgs.PlayerBucket.Clients.DependencyInjection;
using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBucketWorker.Benchmark;

public class Benchmarks
{
	private readonly string _playerCode = Guid.NewGuid().ToString("N");
	private IHost _host;
	private IPlayerBucketClient _playerBucketClient;

	[IterationSetup]
	public void IterationSetup()
	{
		_host = Host.CreateDefaultBuilder()
			.ConfigureServices(services => services
				.AddPlayerBucketClient()
				.AddMessageQueue()
				.AddNatsMessageQueue(config => config
					.ConfigQueueOptions((options, _) => options.Url = "nats://localhost:4222"))
				.AddNatsGlobPatternExchange("*"))
			.Build();

		_playerBucketClient = _host.Services.GetRequiredService<IPlayerBucketClient>();

		_playerBucketClient.CreateBucketAsync(_playerCode, 101M, BucketType.Deposit, Guid.NewGuid().ToString()).AsTask().Wait();
	}

	[Benchmark(Baseline = true)]
	public async Task Scenario1Async()
	{
		for (int i = 0; i < 100; i++)
			await _playerBucketClient.FillInBucketAsync(_playerCode, 1M, FillType.Game, Guid.NewGuid().ToString());
	}
}
