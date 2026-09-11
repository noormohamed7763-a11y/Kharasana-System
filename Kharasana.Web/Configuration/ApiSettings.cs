namespace Kharasana.Web.Configuration
{
    /// <summary>
    /// إعدادات الاتصال بطبقة API. أصل الملفات يُشتق تلقائياً
    /// من <see cref="BaseUrl"/> — لا حاجة لحقل منفصل ينتج تباين مخططات
    /// (Web على HTTPS وملفات على HTTP) أو أصل خاطئ عند تغيّر العنوان.
    /// </summary>
    public class ApiSettings
    {
        /// <summary>عنوان API، مثل <c>http://localhost:5000</c> أو <c>https://localhost:7180</c>.</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>يُشتق من <see cref="BaseUrl"/>: مخطط + مضيف + منفذ فقط.</summary>
        public string FilesOrigin
        {
            get
            {
                if (string.IsNullOrWhiteSpace(BaseUrl))
                    return string.Empty;
                try
                {
                    return new Uri(BaseUrl, UriKind.Absolute).GetLeftPart(UriPartial.Authority);
                }
                catch
                {
                    // قيمة إعداد غير صحيحة — يطبَّع السبب في استدعاء BuildLogoUrl
                    // بدل أن تنهار خدمة الـ DI نفسها.
                    return BaseUrl.TrimEnd('/');
                }
            }
        }
    }
}