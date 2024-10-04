using System.Text.Json.Serialization;
using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PlayerBucketService;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry();

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("Otel:NodeIp")!;
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("Otel:ServiceName")!;
			fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
				"MachineInfo",
				() => new
				{
					Environment.MachineName
				}));
		});
	});

builder.Services
	.AddApiVersioning(versionOptions => versionOptions.AssumeDefaultVersionWhenUnspecified = true)
	.AddApiExplorer(options => options.GroupNameFormat = "'v'VVV")
	.Services
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers()
	.AddMvcOptions(mvcOptions => mvcOptions.AddWebApiToolkitFilters())
	.AddJsonOptions(options => options
		.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
	.AddPlayerBucketService()
	.AddPlayerBucketServiceRepositories((_, dbBuilder) => dbBuilder
		.UseNpgsql("name=Tgs"))
	.AddPlayerBucketClient(clientBuilder => { });

builder.Services
	.AddKeyedSingleton(
		"Nats",
		(sp, _) => new NatsConnection(NatsOpts.Default with
		{
			Url = builder.Configuration.GetConnectionString("Nats")!
		}))
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
		.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats")))
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddSwaggerGen(genOptions =>
	{
		genOptions.OperationFilter<SwaggerDefaultValues>();
		genOptions.ApplyWebApiToolkitSupport();

		var xmlFileNames = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml");
		foreach (var xmlFileName in xmlFileNames)
			genOptions.IncludeXmlComments(xmlFileName);
	})
	.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

builder.Services
	.AddHealthChecks();

var excludeExtensions = new[] { "html", "ico", "png", "jpg" };
builder.Services
	.AddOpenTelemetry()
	.ConfigureResource(_ => _.AddService(builder.Configuration.GetValue<string>("Otel:ServiceName")))
	.WithTracing(tracingBuilder => tracingBuilder
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddSource("Valhalla.*")
		.AddNpgsql()
		.AddAspNetCoreInstrumentation(options => options.Filter =
			httpContext => !httpContext.Request.Path.HasValue
				|| !httpContext.Request.Path.StartsWithSegments("/swagger")
				&& !httpContext.Request.Path.StartsWithSegments("/healthz")
				&& !httpContext.Request.Path.StartsWithSegments("/metrics")
				&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
				&& !excludeExtensions.Any(
					ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
		.AddProfileViewExporter()
		.AddOtlpExporter(options => options.Endpoint = builder.Configuration.GetValue<Uri>("Otel:Collector")))
	.WithMetrics(meterBuiler => meterBuiler
			.AddRuntimeInstrumentation()
			.AddAspNetCoreInstrumentation()
			.AddOtlpExporter())
	.Services
	.AddProfileViewer();

var app = builder.Build();

app.MapControllers();
app.MapSwagger();
app.MapProfileViewer();
app.MapHealthChecks("/healthz");

app.UseSwaggerUI(uiOptions =>
{
	var descriptions = app.DescribeApiVersions();

	// build a swagger endpoint for each discovered API version
	foreach (var description in descriptions)
	{
		var url = $"/swagger/{description.GroupName}/swagger.yaml";
		var name = description.GroupName.ToUpperInvariant();
		uiOptions.SwaggerEndpoint(url, name);
	}
});

SentrySdk.CaptureMessage($"Started result PlayerBucket service at {DateTime.Now.ToString()}");

app.Run();
