using Kharasana.Domain.Enums;
using System.Linq;

namespace Kharasana.Domain.Helpers;

public static class TripStatusHelper
{
    public static TripStatus[] GetAllowedTransitions(TripStatus status) => status switch
    {
        TripStatus.Scheduled => new[] { TripStatus.Departed, TripStatus.Cancelled },
        TripStatus.Departed => new[] { TripStatus.AtSite, TripStatus.Cancelled },
        TripStatus.AtSite => new[] { TripStatus.Unloaded, TripStatus.Cancelled },
        TripStatus.Unloaded => new[] { TripStatus.Completed },
        TripStatus.Completed => System.Array.Empty<TripStatus>(),
        TripStatus.Cancelled => System.Array.Empty<TripStatus>(),
        _ => System.Array.Empty<TripStatus>()
    };

    public static bool CanTransition(TripStatus from, TripStatus to) => GetAllowedTransitions(from).Contains(to);

    public static string GetArabicName(TripStatus s) => s switch
    {
        TripStatus.Scheduled => "مجدولة",
        TripStatus.Departed => "خرجت من المصنع",
        TripStatus.AtSite => "في الموقع",
        TripStatus.Unloaded => "تم التفريغ",
        TripStatus.Completed => "مكتملة",
        TripStatus.Cancelled => "ملغاة",
        _ => "غير معروفة"
    };
}
