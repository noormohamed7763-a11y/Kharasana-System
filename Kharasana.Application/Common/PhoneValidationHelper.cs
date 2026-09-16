using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Interfaces;
using Kharasana.Domain.Common;

namespace Kharasana.Application.Common;

/// <summary>
/// استخراج تطبيع رقم الهاتف+yemeniPhoneHelper مع فحص التفرّد —
/// بلوك واحد مشترك يُستبدل الأربعة المكررة في AuthService و UserService.
/// </summary>
public static class PhoneValidationHelper
{
    /// <summary>
    /// تطبيع رقم الهاتف الوارد عبر YemeniPhoneHelper —
    /// إن فشل يرمي <c>BusinessException(Messages.InvalidYemeniPhone)</c>،
    /// وإن كان فارغاً يُعيد null.
    /// </summary>
    public static string? NormalizeOrThrow(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            return null;

        var normalized = YemeniPhoneHelper.Normalize(rawPhone);
        if (normalized == null)
            throw new BusinessException(Messages.InvalidYemeniPhone);

        return normalized;
    }

    /// <summary>
    /// تطبيع + فحص التفرّد — يعيد null عند الفراغ (أو يحتفظ بالقيمة الحالية)،
    /// ويرمي ConflictException عند التعارض مع رقم آخر مسجّل مسبقاً.
    /// <param name="currentPhone">رقم المستخدم الحالي (ل:]) } ;
    /// </summary>
    public static async Task<string?> NormalizeAndEnsureUniqueAsync(
        IUnitOfWork unitOfWork,
        string? rawPhone,
        string? currentPhone = null)
    {
        var normalized = NormalizeOrThrow(rawPhone);

        // 0 = فارغ — يحتفظ بالقيمة الحالية كما في السلوك الأصلي
        if (normalized == null)
            return currentPhone;

        // لا نفحص التفرّد إن كان رقم المستخدم الحالي هو نفسه
        if (normalized != currentPhone)
        {
            var exists = await unitOfWork.Users.PhoneExistsAsync(normalized);
            if (exists)
                throw new ConflictException(Messages.PhoneAlreadyExists);
        }

        return normalized;
    }
}
