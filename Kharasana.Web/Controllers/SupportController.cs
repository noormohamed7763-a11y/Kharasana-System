using Microsoft.AspNetCore.Mvc;
using Kharasana.Web.Filters;

namespace Kharasana.Web.Controllers
{
    /// <summary>
    /// صفحة الدعم والمساعدة — صفحة ثابتة لموظف المصنع فقط.
    /// تعرض دليل استخدام سريع وأسئلة شائعة ووسائل التواصل مع الدعم،
    /// دون أي بيانات ديناميكية أو استدعاءات API.
    /// </summary>
    [SessionAuthorize("FactoryEmployee")]
    public class SupportController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
