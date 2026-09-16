namespace Kharasana.API.Common;

/// <summary>
/// أسماء الأدوار وتركيباتها كما تُستخدم فعلياً في وسوم [Authorize(Roles = ...)] — مصدر وحيد
/// بدل تكرار السلاسل النصية عبر وحدات التحكم. بنيت التراكيب المركّبة خصيصاً من تراكيب السطور
/// الفعلية الستة الموجودة في كود التحكم (Admin / FactoryEmployee فقط، الثلاثي، الرباعي…).
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