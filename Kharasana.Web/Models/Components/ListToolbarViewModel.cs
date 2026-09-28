namespace Kharasana.Web.Models.Components;

/// <summary>
/// شريط البحث والفلترة الموحّد لصفحات القوائم التي تُصفّي على الخادم
/// (Orders / Clients / Drivers / Users).
///
/// <para>كانت كتلة الشريط نفسها منسوخة حرفياً في كل صفحة: بطاقة
/// <c>form-card</c> + شبكة <c>row g-2</c> + صندوق بحث + حقل <c>pageSize</c> مخفي +
/// زرّا «بحث» و«إعادة تعيين». الاختلاف الوحيد بين النسخ كان نص الـ placeholder
/// وعروض الأعمدة، أي أن أي تعديل على الشريط كان يلزم تكراره في خمسة مواضع
/// وتُنسى إحداها.</para>
///
/// <para><b>ليست لكل صفحات القوائم:</b> صفحة أنواع الخرسانة تُصفّي صفوف الصفحة
/// الحالية في المتصفح (بلا إرسال إلى الخادم)، فإرسالها داخل هذا النموذج يهدم
/// سلوكها؛ ولذلك بقيت بترميزها الخاص.</para>
/// </summary>
public class ListToolbarViewModel
{
    /// <summary>إجراء النموذج الذي يُرسل إليه البحث.</summary>
    public string ActionName { get; set; } = "Index";

    /// <summary>وحدة التحكّم — فارغ يعني الوحدة الحالية.</summary>
    public string? ControllerName { get; set; }

    /// <summary>حجم الصفحة يُحفظ في حقل مخفي حتى لا يضيع عند البحث.</summary>
    public int? PageSize { get; set; }

    public Search.SearchBoxViewModel Search { get; set; } = new();

    /// <summary>عرض عمود البحث في شبكة Bootstrap.</summary>
    public string SearchColumnClass { get; set; } = "col-lg-6";

    /// <summary>فلاتر إضافية (الحالة، المصنع...) تُرسل مع النموذج.</summary>
    public List<ListToolbarFilterViewModel> Filters { get; set; } = new();

    /// <summary>عرض عمود الأزرار. الافتراضي يدفعها إلى الطرف المقابل بلا عمود فاصل فارغ.</summary>
    public string ActionsColumnClass { get; set; } = "col-lg-4 ms-auto";

    public string SubmitText { get; set; } = "بحث";

    public string ResetText { get; set; } = "إعادة تعيين";
}

/// <summary>قائمة منسدلة داخل شريط القوائم.</summary>
public class ListToolbarFilterViewModel
{
    public string Id { get; set; } = string.Empty;

    /// <summary>اسم الحقل في سلسلة الاستعلام. فارغ يعني فلتراً لا يُرسل (يُدار بالـ JS).</summary>
    public string? Name { get; set; }

    /// <summary>وصف يُقرأ لقارئ الشاشة، لأن القائمة نفسها بلا label مرئي.</summary>
    public string AriaLabel { get; set; } = string.Empty;

    public string ColumnClass { get; set; } = "col-lg-3";

    /// <summary>نص خيار «الكل» الفارغ. اتركه فارغاً لقائمة بلا خيار فارغ.</summary>
    public string EmptyOptionText { get; set; } = string.Empty;

    public List<ListToolbarOptionViewModel> Options { get; set; } = new();

    /// <summary>يُرسل النموذج تلقائياً عند تغيير القيمة.</summary>
    public bool AutoSubmit { get; set; } = true;
}

/// <summary>خيار واحد داخل قائمة الفلترة.</summary>
public class ListToolbarOptionViewModel
{
    public string Value { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public bool Selected { get; set; }
}
