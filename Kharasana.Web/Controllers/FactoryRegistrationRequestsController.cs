using Kharasana.Application.Common;
using Kharasana.Domain.Enums;
using Kharasana.Web.Controllers;
using Kharasana.Web.Filters;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.FactoryRegistration;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers
{
    [SessionAuthorize(Roles.Admin)]
[Route("FactoryRegistrationRequests")]
    public class FactoryRegistrationRequestsController : BaseController
    {
        private readonly IFactoryRegistrationRequestApiService _apiService;

        public FactoryRegistrationRequestsController(IFactoryRegistrationRequestApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] RegistrationRequestFilterParams filter)
        {
            var pagedRequests = await _apiService.GetPagedRequestsAsync(filter);

            var pendingCount = await _apiService.GetPendingRequestsCountAsync();
            var approvedCount = await _apiService.GetApprovedRequestsCountAsync();
            var rejectedCount = await _apiService.GetRejectedRequestsCountAsync();

            var vm = new FactoryRegistrationRequestsIndexViewModel
            {
                Filter = filter,
                TotalAllRequestsCount = pendingCount + approvedCount + rejectedCount,
                PendingCount = pendingCount,
                ApprovedCount = approvedCount,
                RejectedCount = rejectedCount,
                PagedRequests = new PagedResult<RegistrationRequestListItemViewModel>
                {
                    Items = pagedRequests.Items.Select(r => new RegistrationRequestListItemViewModel
                    {
                        Id = r.Id,
                        FactoryName = r.FactoryName,
                        OwnerName = r.ContactName,
                        Email = r.ContactEmail,
                        StatusText = r.Status switch
                        {
                            RegistrationStatus.Pending => "قيد الانتظار",
                            RegistrationStatus.Approved => "تم القبول",
                            RegistrationStatus.Rejected => "مرفوض",
                            _ => r.Status.ToString()
                        },
                        StatusClass = r.Status switch
                        {
                            RegistrationStatus.Pending => "status-warning",
                            RegistrationStatus.Approved => "status-success",
                            RegistrationStatus.Rejected => "status-danger",
                            _ => "status-secondary"
                        },
                        CreatedAt = r.CreatedAt
                    }).ToList(),
                    TotalCount = pagedRequests.TotalCount,
                    PageNumber = pagedRequests.PageNumber,
                    PageSize = pagedRequests.PageSize
                }
            };

            return View(vm);
        }

        [HttpGet("Details/{id:int}")]
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
                RequestedConcreteTypes = request.RequestedConcreteTypes,
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