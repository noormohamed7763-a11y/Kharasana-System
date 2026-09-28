namespace Kharasana.Web.Models.Components;

/// <summary>
/// أزرار التواصل مع رقم هاتف: نسخ، اتصال، واتساب.
///
/// <para>كانت هذه المجموعة مكرّرة في ثلاثة مواضع (جدول العملاء، جدول السائقين،
/// تفاصيل الطلب) بثلاث صيغ مختلفة للنسخ والاتصال، وكل موضع يحسب رابط واتساب
/// بطريقته: <c>Phone.Replace("+", "").Replace(" ", "")</c>. هذا الحساب اليدوي
/// ينكسر مع الأرقام المخزّنة بالصيغة المحلية (<c>0771234567</c>) أو بالأرقام
/// العربية-الهندية (<c>٠٧٧١٢٣٤٥٦٧</c>): ينتج رابطاً بلا رمز الدولة فلا يصل
/// لصاحبه. الآن يمر الرقم على <c>YemeniPhoneHelper.Normalize</c> أولاً.</para>
/// </summary>
public class PhoneActionsViewModel
{
    /// <summary>الرقم كما يُعرض ويُنسخ ويُتصل به (بصيغته الخام).</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// اسم صاحب الرقم — يُضاف إلى وصف قارئ الشاشة ليعرف المستخدم لمن يخص الزر
    /// حين يتنقل بين صفوف الجدول. اتركه فارغاً ليُستعمل <see cref="ContactLabel"/>.
    /// </summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>وصف صاحب الرقم: «العميل»، «السائق»...</summary>
    public string ContactLabel { get; set; } = "الجهة";

    /// <summary>زر الاتصال الهاتفي. يُخفى في السياقات التي لا يُتصل فيها بالرقم.</summary>
    public bool ShowCall { get; set; } = true;

    public bool ShowWhatsApp { get; set; } = true;

    /// <summary>نص رسالة واتساب الجاهزة. يُرمَّز داخل القالب فلا تُرمّزه في الصفحة.</summary>
    public string? WhatsAppMessage { get; set; }

    public PhoneActionsVariant Variant { get; set; } = PhoneActionsVariant.Table;

    /// <summary>دفع الأزرار إلى الطرف المقابل داخل خلية الجدول.</summary>
    public bool AlignEnd { get; set; } = true;

    /// <summary>ما يُعرض عند غياب الرقم.</summary>
    public string EmptyText { get; set; } = "-";
}

public enum PhoneActionsVariant
{
    /// <summary>داخل خلايا الجداول: أزرار روابط صغيرة بلا إطار.</summary>
    Table,

    /// <summary>داخل بطاقات التفاصيل: أزرار محدودة بإطار.</summary>
    Outline
}
