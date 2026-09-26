using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Persistence;

/// <summary>
/// كشف انتهاك قيود التفرّد (Unique Index/Constraint) في SQL Server تحديدًا —
/// ليُترجم إلى 409 بدل 500. الفحص ضيّق عمدًا: أخطاء المفاتيح الأجنبية
/// وأخطاء قاعدة البيانات الأخرى لا تُعتبر تكرارًا وتمرّ كما هي.
/// </summary>
public static class UniqueConstraintDetector
{
    /// <summary>2601: محاولة إدراج مفتاح مكرر في فهرس فريد.</summary>
    private const int DuplicateKeyRowInUniqueIndex = 2601;

    /// <summary>2627: انتهاك قيد UNIQUE.</summary>
    private const int UniqueConstraintViolation = 2627;

    /// <summary>هل رقم خطأ SQL Server يمثّل انتهاك تفرّد؟</summary>
    public static bool IsUniqueViolation(int sqlErrorNumber)
        => sqlErrorNumber is DuplicateKeyRowInUniqueIndex or UniqueConstraintViolation;

    /// <summary>
    /// هل الاستثناء ناتج عن انتهاك تفرّد في SQL Server؟
    /// أي مزوّد آخر (InMemory مثلاً) أو أي خطأ آخر يُعيد false.
    /// </summary>
    public static bool IsUniqueViolation(Exception exception)
    {
        if (exception is DbUpdateException { InnerException: SqlException sqlException })
        {
            foreach (SqlError error in sqlException.Errors)
            {
                if (IsUniqueViolation(error.Number))
                    return true;
            }
        }

        return false;
    }
}
