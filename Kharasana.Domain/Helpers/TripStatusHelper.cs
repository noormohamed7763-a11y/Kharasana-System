using Kharasana.Domain.Enums;

namespace Kharasana.Domain.Helpers;

public static class TripStatusHelper
{
    public static string GetArabicName(TripStatus s) => s switch
    {
        TripStatus.Scheduled => "مجدولة",
        TripStatus.Departed => "خرجت من المصنع",
        TripStatus.OnTheWay => "في الطريق",
        TripStatus.AtSite => "في الموقع",
        TripStatus.Unloaded => "تم التفريغ",
        TripStatus.Completed => "مكتملة",
        TripStatus.Cancelled => "ملغاة",
        _ => "غير معروف"
    };

    public static TripStatus[] GetAllowed(TripStatus current) => current switch
    {
        TripStatus.Scheduled => new[] { TripStatus.Departed, TripStatus.Cancelled },
        TripStatus.Departed => new[] { TripStatus.AtSite, TripStatus.Cancelled },
        TripStatus.AtSite => new[] { TripStatus.Unloaded, TripStatus.Cancelled },
        TripStatus.Unloaded => new[] { TripStatus.Completed },
        TripStatus.Completed => Array.Empty<TripStatus>(),
        TripStatus.Cancelled => Array.Empty<TripStatus>(),
        _ => Array.Empty<TripStatus>()
    };

    public static bool CanTransition(TripStatus from, TripStatus to)
        => GetAllowed(from).Contains(to);
}
