using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Service.PlayerBuckets;

public static class PlayerBucketUtility
{
	public static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		Converters = {
			new JsonStringEnumConverter()
		}
	};
}
