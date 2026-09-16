namespace Kharasana.Application.Common;

/// <summary>
/// أسماء الأدوار وتركيباتها كما تُستخدم فعلياً عبر طبقات النظام — مصدر وحيد
/// بدل تكرار السلاسل النصية. تستخدمها طبقة API في وسوم [Authorize(Roles = ...)]
/// وطبقة Web في وسوم SessionAuthorize وفي مقارنات الدور (Role == Roles.X).
/// بنيت التركيبات المركّبة خصيصاً من تراكيب السطور الفعلية الموجودة في كود التحكم.
/// </summary>
public static class Roles
{
    // أدوار فردية
    public const string Admin = "Admin";
    public const string FactoryEmployee = "FactoryEmployee";
    public const string Driver = "Driver";
    public const string Client = "Client";

    // التركيبات الفعلية الموجودة في [Authorize(Roles = ...)] — الفاصلة تعني «أو»
    public const string AdminOrFactoryEmployee = "Admin,FactoryEmployee";
    public const string AdminOrFactoryEmployeeOrClient = "Admin,FactoryEmployee,Client";
    public const string AdminOrFactoryEmployeeOrClientOrDriver = "Admin,FactoryEmployee,Client,Driver";
    public const string AdminOrFactoryEmployeeOrDriver = "Admin,FactoryEmployee,Driver";
}