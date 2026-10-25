using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.RawMaterials;
using Microsoft.AspNetCore.Mvc;
using Kharasana.Application.Common;

namespace Kharasana.Web.Controllers;

public class RawMaterialsController : BaseController
{
    private readonly IRawMaterialService _rawMaterialService;

    public RawMaterialsController(IRawMaterialService rawMaterialService)
    {
        _rawMaterialService = rawMaterialService;
    }

    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
    {
        var result = await _rawMaterialService.GetPagedAsync(pageNumber, pageSize, searchTerm);
        ViewBag.SearchTerm = searchTerm;
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var rawMaterial = await _rawMaterialService.GetByIdAsync(id);
        if (rawMaterial == null)
        {
            return NotFound();
        }
        return View(rawMaterial);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRawMaterialViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var success = await _rawMaterialService.CreateAsync(viewModel);
        if (!success)
        {
            TempData[TempDataError] = "حدث خطأ أثناء إنشاء المادة الخام";
            return View(viewModel);
        }

        TempData[TempDataSuccess] = "تم إنشاء المادة الخام بنجاح";
        return RedirectToAction(nameof(Index));
    }
}