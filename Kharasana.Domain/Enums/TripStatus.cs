using System.Text.Json.Serialization;

namespace Kharasana.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TripStatus
{
    Scheduled = 0,
    Departed = 1,
    OnTheWay = 2,
    AtSite = 3,
    Unloaded = 4,
    Completed = 5,
    Cancelled = 6
}
