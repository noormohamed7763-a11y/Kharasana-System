using Xunit;

namespace Kharasana.IntegrationTests.Infrastructure;

/// <summary>
/// <c>[Fact]</c> لا يُنفَّذ إلا عند تكوين SQL Server.
///
/// <para><b>لماذا سمة مخصّصة ولا <c>if (!IsConfigured) return;</c> في متن الاختبار:</b>
/// الاختبار الذي يعود فورًا يُسجَّل <b>ناجحًا</b> في التقرير، فيُقرأ «مُرّ» وهو لم
/// يقس شيئًا — وهذا أسوأ من الفشل لأنه يُطمئن كذبًا. و<c>xunit</c> يقرأ خاصية
/// <c>Skip</c> وقت <b>الاستكشاف</b>، فيظهر الاختبار «متخطّى» صراحةً بسبب مكتوب.</para>
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!SqlServerTestEnvironment.IsConfigured)
        {
            Skip = SqlServerTestEnvironment.SkipReason;
        }
    }
}
