namespace Kharasana.Application.Interfaces.Services;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);

    /// <summary>
    /// ينفّذ عملية تحقّق كاملة ثم يُهمل نتيجتها — تُستدعى عند عدم وجود مستخدم مطابق
    /// ليكون زمن الردّ مقارباً لزمن «المستخدم موجود وكلمة المرور خاطئة».
    ///
    /// <para>بدونها يردّ مسار «لا يوجد مستخدم» فوراً بينما المسار الآخر يكلّف عملية
    /// تجزئة كاملة، فيُستدلّ على وجود الحساب من زمن الاستجابة وحده — حتى لو تطابقت
    /// نصوص الرسائل.</para>
    /// </summary>
    void VerifyDummy(string password);
}