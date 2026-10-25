using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.ProductionBatches;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers;

public class ProductionBatchesController : BaseController
{
    private readonly IProductionBatchService _productionBatchService;

    public ProductionBatchesController(IProductionBatchService productionBatchService)
    {
        _productionBatchService = productionBatchService;
    }

    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
    {
        var result = await _productionBatchService.GetPagedAsync(pageNumber, pageSize, searchTerm);
        ViewBag.SearchTerm = searchTerm;
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var batch = await _productionBatchService.GetByIdAsync(id);
        if (batch == null)
        {
            return NotFound();
        }
        return View(batch);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductionBatchViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var success = await _productionBatchService.CreateAsync(viewModel);
        if (!success)
        {
            TempData[TempDataError] = "حدث خطأ أثناء إنشاء دفعة الإنتاج";
            return View(viewModel);
        }

        TempData[TempDataSuccess] = "تم إنشاء دفعة الإنتاج بنجاح";
        return RedirectToAction(nameof(Index));
    }
}
