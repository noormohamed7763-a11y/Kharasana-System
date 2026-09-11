using System.ComponentModel.DataAnnotations;

namespace Kharasana.Domain.Enums;

public enum SlabType
{
    [Display(Name = "أساسات")]
    Foundation = 0,

    [Display(Name = "أعمدة")]
    Columns = 1,

    [Display(Name = "كمرات")]
    Beams = 2,

    [Display(Name = "سقف")]
    Roof = 3,

    [Display(Name = "أخرى")]
    Other = 4
}