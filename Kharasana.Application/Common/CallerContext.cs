using Kharasana.Domain.Enums;

namespace Kharasana.Application.Common;

/// <summary>
/// هوية الطالب المستخرجة من جلسة JWT — قطعة بيانات واحدة تُمرَّر لأعمال التحكم
/// بدل تمرير (callerId, callerRole, callerFactoryId) متفرقة.
/// </summary>
/// <param name="UserId">معرف المستخدم الموثَّق.</param>
/// <param name="Role">دوره.</param>
/// <param name="FactoryId">معرف المصنع إن كان للدور مصنع (موظف مصنع/سائق)، وإلا null.</param>
public sealed record CallerContext(int UserId, UserRole Role, int? FactoryId);