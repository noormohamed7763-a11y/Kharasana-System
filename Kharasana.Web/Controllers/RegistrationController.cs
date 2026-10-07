using Kharasana.Web.Services.Interfaces;
using Kharasana.Application.DTOs.Factory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers
{
    [AllowAnonymous]
    public class RegistrationController : Controller
    {
        private readonly IFactoryRegistrationRequestApiService _apiService;

        public RegistrationController(IFactoryRegistrationRequestApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new RegisterFactoryDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterFactoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            await _apiService.RegisterAsync(dto);

            TempData[BaseController.TempDataSuccess] = "تم إرسال طلب تسجيل المصنع بنجاح، سيتم مراجعته من قبل الإدارة.";
            return RedirectToAction("Login", "Account");
        }
    }
}