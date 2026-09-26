using Kharasana.Application.Interfaces.Services;

namespace Kharasana.Infrastructure.Authentication;

public class PasswordHasher : IPasswordHasher
{
    /// <summary>
    /// تجزئة ثابتة تُولَّد مرة واحدة عند أول استخدام، ولا يمكن أن تطابق كلمة مرور
    /// حقيقية لأن مصدرها قيمة عشوائية. تُولَّد وقت التشغيل بدل كتابتها نصاً ثابتاً
    /// في الكود لتتبع معامل الكلفة المُعدّ في BCrypt تلقائياً — فلو غُيّر، بقي
    /// زمن <see cref="VerifyDummy"/> مساوياً لزمن التحقّق الحقيقي.
    /// </summary>
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public void VerifyDummy(string password)
    {
        BCrypt.Net.BCrypt.Verify(password, DummyHash);
    }
}