using System.ComponentModel.DataAnnotations;

namespace Kharasana.Domain.Enums;

public enum AggregateSize
{
    [Display(Name = "حد أقصى 20 ملم")]
    Size20mm = 0,

    [Display(Name = "حد أقصى 12 ملم")]
    Size12mm = 1
}

