using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBomber.CSharp;
using Tgs.PlayerBucket.Clients.DependencyInjection;
using Tgs.Service.PlayerBuckets;

var builder = Host.CreateDefaultBuilder(args)
	.ConfigureLogging(logging => logging.ClearProviders())
	.ConfigureServices(services => services
	.AddPlayerBucketClient(httpClientBuilder => httpClientBuilder
		.ConfigureHttpClient(http => http.BaseAddress =
			new Uri("http://localhost:5800")))
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
		.ConfigQueueOptions((options, _) => options.Url = "nats://localhost:5222"))
	.AddNatsGlobPatternExchange("*"));

var app = builder.Build();

var players = Enumerable.Range(0, 10000).Select(n => Guid.NewGuid().ToString("N")).ToArray();

var client = app.Services.GetRequiredService<IPlayerBucketClient>();

//var createBucketStep = Step.Create("Create Bucket", async context =>
//{
//	var playerCode = Guid.NewGuid().ToString("N");
//	_ = await client.CreateBucketAsync(
//		playerCode,
//		100M,
//		BucketType.Deposit,
//		Guid.NewGuid().ToString()).ConfigureAwait(false);

//	context.Data.Add("player", playerCode);

//	return Response.Ok(statusCode: 200);
//}, timeout: TimeSpan.FromSeconds(10));

//var queryStep = Step.Create("Get Bucket Amount", async context =>
//{
//	var playerCode = (context.Data.GetValueOrDefault("player") as string)!;

//	var result = await client.ReadBucketsAsync(playerCode).ConfigureAwait(false);

//	return result.Amount == 10M ? Response.Ok(statusCode: 200) : Response.Fail();
//}, timeout: TimeSpan.FromSeconds(1D));

var bucketScenario = Scenario.Create("Bucket Test",
	async context =>
	{
		var playerCode = players[Random.Shared.Next(players.Length - 1)];

		await client.FillInBucketAsync(
			playerCode,
			1M,
			FillType.Game,
			Guid.NewGuid().ToString()).ConfigureAwait(false);

		return Response.Ok();
	})
	.WithInit(async ctx =>
	{
		var batchs = players.Select((id, index) => (id, index))
			.GroupBy(t => Math.DivRem(t.index, 10, out _))
			.Select(g => g.Select(t => t.id).ToArray());
		foreach (var b in batchs)
		{
			_ = await Task.WhenAll(b.Select(id => client.CreateBucketAsync(
				id,
				10000M,
				BucketType.Deposit,
				Guid.NewGuid().ToString()).AsTask()).ToArray()).WaitAsync(TimeSpan.FromSeconds(10D));
		}
	})
	.WithoutWarmUp()
	.WithLoadSimulations(
		Simulation.Inject(100, TimeSpan.FromSeconds(1D), TimeSpan.FromSeconds(10)),
		Simulation.Inject(500, TimeSpan.FromSeconds(1D), TimeSpan.FromSeconds(10)),
		Simulation.Inject(1000, TimeSpan.FromSeconds(1D), TimeSpan.FromSeconds(10)),
		Simulation.Inject(2000, TimeSpan.FromSeconds(1D), TimeSpan.FromSeconds(10)));

NBomberRunner.RegisterScenarios(bucketScenario).Run();
