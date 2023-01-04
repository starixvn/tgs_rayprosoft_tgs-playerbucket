using Microsoft.AspNetCore.Mvc;

namespace PlayerBucketService.ViewModels;

public class PlayerBucketEventQueryViewModel
{
	[FromQuery(Name = "date")]
	public DateTime? QueryDate { get; set; }

	[FromQuery(Name = "id")]
	public Guid? EventId { get; set; }
}
