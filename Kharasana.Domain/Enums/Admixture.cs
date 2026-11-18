using System.ComponentModel.DataAnnotations;

namespace Kharasana.Domain.Enums;

public enum Admixture
{
    [Display(Name = "بدون إضافات")]
    None = 0,

    [Display(Name = "لدانة (تسهيل الضخ)")]
    Plasticizer = 1,

    [Display(Name = "مؤخر شك")]
    Retarder = 2
}

