using Kharasana.Domain.Enums;
using Kharasana.Web.Controllers;
using Kharasana.Web.Filters;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.FactoryRegistration;
using Kharasana.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers
{
    [SessionAuthorize(Roles.Admin)]
    public class FactoryRegistrationRequestsController : BaseController
    {
        private readonly IFactoryRegistrationRequestApiService _apiService;

        public FactoryRegistrationRequestsController(IFactoryRegistrationRequestApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _apiService.GetPendingAsync();

            var vm = new FactoryRegistrationRequestsIndexViewModel
            {
                PagedRequests = new PagedResult<RegistrationRequestListItemViewModel>
                {
                    Items = requests.Select(r => new RegistrationRequestListItemViewModel
                    {
                        Id = r.Id,
                        FactoryName = r.FactoryName,
                        OwnerName = r.ContactName,
                        Email = r.ContactEmail,
                        StatusText = r.Status.ToString(),
                        StatusClass = r.Status switch
                        {
                            RegistrationStatus.Pending => "bg-warning",
                            RegistrationStatus.Approved => "bg-success",
                            RegistrationStatus.Rejected => "bg-danger",
                            _ => "bg-secondary"
                        },
                        CreatedAt = r.CreatedAt
                    }).ToList(),
                    TotalCount = requests.Count()
                }
            };

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _apiService.GetByIdAsync(id);
            if (request == null) return NotFound();

            var vm = new RegistrationRequestDetailsViewModel
            {
                Id = request.Id,
                FactoryName = request.FactoryName,
                Area = request.Area,
                Address = request.Address,
                BusinessRegistrationNumber = request.CommercialId,
                OwnerName = request.ContactName,
                Email = request.ContactEmail,
                Phone = request.ContactPhone,
                Status = request.Status,
                CreatedAt = request.CreatedAt
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _apiService.ApproveAsync(id);
            if (result.Succeeded) return RedirectToAction(nameof(Index));
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var result = await _apiService.RejectAsync(id, reason);
            if (result.Succeeded) return RedirectToAction(nameof(Index));
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}