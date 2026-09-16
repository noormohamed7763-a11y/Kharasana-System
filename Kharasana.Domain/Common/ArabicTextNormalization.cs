using System.Linq.Expressions;
using System.Reflection;

namespace Kharasana.Domain.Common;

/// <summary>
/// بناء التطبيع العربي بوجهين:
/// <list type="bullet">
/// <item><description>مرحلة التشغيل — <see cref="Normalize"/> لتطبيع نص حر خارج قاعدة البيانات.</description></item>
/// <item><description>مرحلة قاعدة البيانات — منشئات تعبيرات <c>Expression&lt;Func&lt;TEntity, bool&gt;&gt;</c>
/// تصف نفس سلاسل <c>Replace</c> بدل تكرارها يدوياً في كل مستودع،
/// لأن استدعاء دالة ثابتة داخل Expression Tree لا تُترجم في EF.</description></item>
/// </list>
/// هذا الفصل الجديد لا يحل محل ArabicTextNormalizer الحالي؛ بل يوفّر ما ينقصه (البناء للتعبيرات).
/// </summary>
public static class ArabicTextNormalization
{
    /// <summary>
    /// أزواج الاستبدال المعتمدة — نفس قيم ArabicTextNormalizer تبقى هنا بمرجع واحد للمستودعات
    /// التي تبني تعبيرات EF (لا تستطيع استدعاء Normalize مباشرة).
    /// </summary>
    private static readonly (string From, string To)[] Replacements =
    {
        ("أ", "ا"),
        ("إ", "ا"),
        ("آ", "ا"),
        ("ى", "ي"),
        ("ة", "ه")
    };

    private static readonly MethodInfo StringReplace =
        typeof(string).GetMethod(nameof(string.Replace), new[] { typeof(string), typeof(string) })!;

    private static readonly MethodInfo StringContains =
        typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

    /// <summary>
    /// تطبيع نص حر للاستخدام في مرحلة التشغيل (مطابق لسلوك ArabicTextNormalizer).
    /// لا تُستخدم هذه الدالة داخل تعبيرات EF.
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var result = text.Trim();
        foreach (var (from, to) in Replacements)
            result = result.Replace(from, to);

        return result;
    }

    /// <summary>
    /// تعبير <c>«الحقل الفصلي» بعد سلسلة Replace تحتوي على النصّ المعياري</c> —
    /// يطابق بالضبط سلسلة
    /// <c>.Replace("أ","ا").Replace("إ","ا").Replace("آ","ا").Replace("ى","ي").Replace("ة","ه").Contains(term)</c>
    /// المتكرّرة في المستودعات، وقابلة للترجمة في EF Core.
    /// </summary>
    /// <param name="selector">إسقاط الحقل النصي (مثل <c>u =&gt; u.FullName</c>).</param>
    /// <param name="normalizedTerm">النص المطلوب بعد تطبيعه مسبقاً عبر <see cref="Normalize"/>.</param>
    public static Expression<Func<TEntity, bool>> Contains<TEntity>(
        Expression<Func<TEntity, string?>> selector,
        string? normalizedTerm)
    {
        if (string.IsNullOrEmpty(normalizedTerm))
            return _ => false;

        Expression body = selector.Body;
        foreach (var (from, to) in Replacements)
        {
            body = Expression.Call(body, StringReplace, Expression.Constant(from), Expression.Constant(to));
        }

        body = Expression.Call(body, StringContains, Expression.Constant(normalizedTerm));
        return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
    }

    /// <summary>
    /// «و» بين شرطين ينتميان لنفس الكيان — يعيد ربط معامل الشرط الثاني بمعامل الأول
    /// حتى يعمل مع مترجم ExpressionTrees الحالي من المستودعات.
    /// </summary>
    public static Expression<Func<TEntity, bool>> And<TEntity>(
        Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right)
        => Compose(left, right, Expression.AndAlso);

    /// <summary>
    /// «أو» بين شرطين ينتميان لنفس الكيان — يعيد ربط معامل الشرط الثاني بمعامل الأول
    /// (يُلئم المركّبات مثل [الاسم أَو الهاتف أَو رقم الطلب]).
    /// </summary>
    public static Expression<Func<TEntity, bool>> Or<TEntity>(
        Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right)
        => Compose(left, right, Expression.OrElse);

    private static Expression<Func<TEntity, bool>> Compose<TEntity>(
        Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right,
        Func<Expression, Expression, BinaryExpression> combinator)
    {
        var rebinder = new ParameterRebinder(left.Parameters[0]);
        var rightBody = rebinder.Visit(right.Body);

        return Expression.Lambda<Func<TEntity, bool>>(
            combinator(left.Body, rightBody),
            left.Parameters);
    }

    private sealed class ParameterRebinder : ExpressionVisitor
    {
        private readonly ParameterExpression _target;

        internal ParameterRebinder(ParameterExpression target) => _target = target;

        protected override Expression VisitParameter(ParameterExpression node) => _target;
    }
}