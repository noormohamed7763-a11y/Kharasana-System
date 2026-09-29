using Kharasana.Web.Configuration;
using Kharasana.Web.Controllers;

namespace Kharasana.Web.Common
{
    /// <summary>
    /// كل ما يتعلّق بملف شعار المصنع في طبقة الويب: قواعد الرفع المبكّرة، وبناء رابط عرضه.
    /// </summary>
    /// <remarks>
    /// <para>كانت قواعد الامتداد والحجم مكرّرة حرفيًا في ثلاثة مواضع
    /// (<see cref="FilesController"/> و<c>FactoryApiService</c> و<c>SettingsApiService</c>)،
    /// و<see cref="BuildUrl"/> نسختين متطابقتيْن — فأي تغيير كان يحتاج تعديلات متطابقة في
    /// مواضع متباعدة، ونسيان أحدها يعني قبول الويب ملفًا يرفضه الخادم.</para>
    ///
    /// <para>⚠️ هذه فحص <b>مبكّر</b> لتجربة المستخدم فقط؛ المرجع النهائي الذي يرفض فعلًا هو
    /// <c>ImageStorageService</c> في طبقة Infrastructure. ولا يمكن استيراد ثوابته لأن
    /// <c>Kharasana.Web</c> لا يشير إلى <c>Kharasana.Infrastructure</c> (ترتيب مقصود) —
    /// فإذا تغيّرت القائمة هناك وجب تغييرها هنا أيضًا.</para>
    /// </remarks>
    public static class LogoFiles
    {
        /// <summary>الامتدادات المقبولة — مطابقة لـ<c>ImageStorageService.AllowedExtensions</c>.</summary>
        public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>الحد الأقصى للحجم — مطابق لـ<c>ImageStorageService.MaxFileSizeInBytes</c>.</summary>
        public const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

        /// <summary>
        /// يبني رابط عرض الشعار من القيمة التي يعيدها الـ API، أو <c>null</c> إن كانت غير صالحة.
        ///
        /// <para>الرابط المطلق من أصل الـ API نفسه يُعاد توجيهه عبر وسيط نفس الأصل
        /// (<see cref="FilesController"/>) ليبقى <c>img-src 'self'</c> في CSP سليماً،
        /// والرابط المطلق لأصل خارجي (نادر) يمرّ كما هو.</para>
        /// </summary>
        public static string? BuildUrl(string? logo, ApiSettings apiSettings)
        {
            if (string.IsNullOrWhiteSpace(logo))
                return null;

            // الـ DTO يعيد رابطاً مطلقاً الآن (مطلب الرابط المطلق)
            if (logo.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                // إن كان الرابط من أصل الـ API نفسه، نعيد توجيهه عبر وسيط نفس الأصل
                // (FilesController) ليبقى img-src 'self' في CSP سليماً.
                if (Uri.TryCreate(logo, UriKind.Absolute, out var absolute)
                    && Uri.TryCreate(apiSettings.FilesOrigin, UriKind.Absolute, out var origin)
                    && string.Equals(absolute.Authority, origin.Authority, StringComparison.OrdinalIgnoreCase))
                {
                    var path = absolute.AbsolutePath;
                    return path.StartsWith("/", StringComparison.Ordinal) ? $"/Files/factories{path}" : null;
                }

                // رابط مطلق لأصل خارجي (نادر) — نمرّره كما هو
                return logo;
            }

            // دفاع ضد قيم غير صالحة في DB (اسم ملف عارٍ بلا "/")
            if (!logo.StartsWith("/", StringComparison.Ordinal))
                return null;

            // الآن نخدم الملفات عبر FilesController على نفس أصل الويب
            // المسار المتوقع في العروض: /Files/factories/Images/Factories/...
            return $"/Files/factories{logo}";
        }
    }
}
