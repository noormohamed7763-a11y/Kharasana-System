namespace Kharasana.Web.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        /// <summary>رمز الحالة (500 / 404 / 403) لاختيار شكل الصفحة المناسب.</summary>
        public int? StatusCode { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}