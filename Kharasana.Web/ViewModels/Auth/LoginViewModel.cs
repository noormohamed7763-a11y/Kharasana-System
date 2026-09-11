using System.ComponentModel.DataAnnotations;

namespace Kharasana.Web.ViewModels.Auth
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "يرجى إدخال البريد الإلكتروني أو رقم الهاتف.")]
        [Display(Name = "البريد الإلكتروني أو رقم الهاتف")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال كلمة المرور.")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "تذكرني")]
        public bool RememberMe { get; set; }
    }
}