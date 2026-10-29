using System.ComponentModel.DataAnnotations;

namespace Kharasana.Domain.Enums;

public enum CementType
{
    [Display(Name = "عادي (بورتلاندي عادي - OPC)")]
    Ordinary = 0,

    [Display(Name = "مقاوم للملوحة (بورتلاندي مقاوم - SRC)")]
    SulphateResistant = 1
}

