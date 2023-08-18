using Microsoft.AspNetCore.Mvc;
using Tgs.Service.PlayerBuckets.ApplicationServices;
using Tgs.ServiceUtility;

namespace PlayerBucketService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandController : ControllerBase
{
	[HttpPut("{brandId:Guid}", Name = "SetBrandBucketTypes")]
	public ValueTask SetBrandBucketTypesAsync(
		[FromServices] ICommandService<SetBrandBucketTypesCommand> commandService,
		Guid brandId,
		string[] bucketTypes)
	{
		var command = new SetBrandBucketTypesCommand(brandId, bucketTypes);

		return commandService.ExecuteAsync(command, HttpContext.RequestAborted);
	}

	[HttpGet("{brandId:Guid}", Name = "GetBrandBucketTypes")]
	public IAsyncEnumerable<string> GetBrandBucketTypesAsync(
		[FromServices] IQueryService<GetBrandBucketTypesQuery, string> queryService,
		Guid brandId)
	{
		var query = new GetBrandBucketTypesQuery(brandId);

		return queryService.QueryAsync(query, HttpContext.RequestAborted);
	}
}
