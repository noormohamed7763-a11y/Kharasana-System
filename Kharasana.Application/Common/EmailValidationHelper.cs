using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Interfaces;

namespace Kharasana.Application.Common;

/// <summary>
/// تطبيع البريد الإلكتروني وفحص تفرّده — مصدر واحد يطابق ما يفعله مسار الدخول.
///
/// <para>العلّة التي يمنعها: الدخول يقصّ المسافات الطرفية من المُعرّف
/// (<c>request.EmailOrPhone?.Trim()</c>) بينما التسجيل كان يخزّن البريد كما وصل،
/// فيُحفظ <c>" ali@x.com"</c> بمسافته ويستحيل الدخول بـ <c>"ali@x.com"</c> أبداً —
/// ولا يكشفه فحص التفرّد لأن البريدين نصّان مختلفان.</para>
///
/// <para>القصّ وحده كافٍ للمطابقة: عمود البريد في SQL Server يحمل ترتيباً غير حسّاس
/// لحالة الأحرف، فاختلاف الحالة يعمل أصلاً. لا نُغيّر حالة الأحرف حتى لا نُعيد كتابة
/// بيانات المستخدمين المخزّنة.</para>
/// </summary>
public static class EmailValidationHelper
{
    /// <summary>
    /// تطبيع البريد: قصّ المسافات الطرفية، والنص الفارغ أو المسافات وحدها تصير null
    /// (لا بريد) بدل نص أبيض يُخزَّن ويُفسد فحص التفرّد لاحقاً.
    /// </summary>
    public static string? Normalize(string? rawEmail)
        => string.IsNullOrWhiteSpace(rawEmail) ? null : rawEmail.Trim();

    /// <summary>
    /// تطبيع + فحص التفرّد — يعيد null عند الفراغ،
    /// ويرمي <c>ConflictException</c> عند التعارض مع بريد مستخدم آخر.
    /// </summary>
    /// <param name="unitOfWork">وحدة العمل المستخدمة لاستعلام المستخدمين.</param>
    /// <param name="rawEmail">البريد الخام الوارد في الطلب.</param>
    /// <param name="excludeUserId">المستخدم الحالي — يُستثنى من الفحص عند التعديل.</param>
    public static async Task<string?> EnsureUniqueAsync(
        IUnitOfWork unitOfWork,
        string? rawEmail,
        int? excludeUserId = null)
    {
        var normalized = Normalize(rawEmail);

        if (normalized == null)
            return null;

        var exists = await unitOfWork.Users.EmailExistsAsync(normalized, excludeUserId);
        if (exists)
            throw new ConflictException(Messages.EmailAlreadyExists);

        return normalized;
    }
}
