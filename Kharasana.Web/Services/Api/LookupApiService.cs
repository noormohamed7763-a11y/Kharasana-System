using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Shared;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.DTOs.User;  // ✅ أضف هذا
using Kharasana.Domain.Enums;           // ✅ أضف هذا
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kharasana.Web.Services.Api
{
    public class LookupApiService : ILookupApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<LookupApiService> _logger;

        public LookupApiService(ApiClient apiClient, ILogger<LookupApiService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        /// <summary>
        /// جلب جميع السائقين المتاحين في النظام
        /// </summary>
        public async Task<List<LookupDto>> GetAvailableDriversAsync()
        {
            try
            {
                _logger.LogInformation("🔍 جلب السائقين المتاحين...");

                // ✅ استخدام PagedResult<UserDto> لأن الـ API يعيد بهذا الشكل
                var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(
                    "Users?role=2&driverStatus=0&PageSize=100");

                if (response != null && response.Success && response.Data?.Items != null)
                {
                    var drivers = response.Data.Items
                        .Where(u => u.IsActive)  // فقط السائقين النشطين
                        .Select(u => new LookupDto
                        {
                            Id = u.UserId,
                            Name = u.FullName,
                            FactoryId = u.FactoryId
                        })
                        .ToList();

                    _logger.LogInformation("✅ تم العثور على {Count} سائقين متاحين", drivers.Count);

                    foreach (var driver in drivers)
                    {
                        _logger.LogInformation("   السائق: معرف={Id}, الاسم={Name}, المصنع={FactoryId}", driver.Id, driver.Name, driver.FactoryId);
                    }

                    return drivers;
                }

                _logger.LogWarning("⚠️ لم يتم العثور على سائقين متاحين.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب السائقين المتاحين");
            }

            return new List<LookupDto>();
        }

        /// <summary>
        /// جلب السائقين المتاحين في مصنع معين فقط
        /// </summary>
        public async Task<List<LookupDto>> GetAvailableDriversByFactoryAsync(int factoryId)
        {
            try
            {
                _logger.LogInformation("🔍 جلب السائقين المتاحين للمصنع {FactoryId}", factoryId);

                // ✅ استخدام PagedResult<UserDto> مع فلتر المصنع
                var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(
                    $"Users?role=2&driverStatus=0&factoryId={factoryId}&PageSize=100");

                if (response != null && response.Success && response.Data?.Items != null)
                {
                    var drivers = response.Data.Items
                        .Where(u => u.IsActive && u.FactoryId == factoryId)
                        .Select(u => new LookupDto
                        {
                            Id = u.UserId,
                            Name = u.FullName,
                            FactoryId = u.FactoryId
                        })
                        .ToList();

                    _logger.LogInformation("✅ تم العثور على {Count} سائقين للمصنع {FactoryId}", drivers.Count, factoryId);

                    foreach (var driver in drivers)
                    {
                        _logger.LogInformation("   السائق: معرف={Id}, الاسم={Name}, المصنع={FactoryId}", driver.Id, driver.Name, driver.FactoryId);
                    }

                    return drivers;
                }

                _logger.LogWarning("⚠️ لم يتم العثور على سائقين للمصنع {FactoryId}", factoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب السائقين للمصنع {FactoryId}", factoryId);
            }

            return new List<LookupDto>();
        }

        /// <summary>
        /// جلب جميع العملاء
        /// </summary>
        public async Task<List<LookupDto>> GetClientsAsync()
        {
            try
            {
                _logger.LogInformation("🔍 جلب العملاء...");

                // ✅ Client = 3 في الـ Enum
                var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(
                    "Users?role=3&PageSize=100");

                if (response != null && response.Success && response.Data?.Items != null)
                {
                    var clients = response.Data.Items
                        .Where(u => u.IsActive)
                        .Select(u => new LookupDto
                        {
                            Id = u.UserId,
                            Name = u.FullName,
                            FactoryId = u.FactoryId
                        })
                        .ToList();

                    _logger.LogInformation("✅ تم العثور على {Count} عملاء", clients.Count);
                    return clients;
                }

                _logger.LogWarning("⚠️ لم يتم العثور على عملاء.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب العملاء");
            }

            return new List<LookupDto>();
        }

        /// <summary>
        /// جلب جميع السائقين (بغض النظر عن حالتهم)
        /// </summary>
        public async Task<List<LookupDto>> GetDriversAsync()
        {
            try
            {
                _logger.LogInformation("🔍 جلب جميع السائقين...");

                // ✅ Driver = 2 في الـ Enum
                var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(
                    "Users?role=2&PageSize=100");

                if (response != null && response.Success && response.Data?.Items != null)
                {
                    var drivers = response.Data.Items
                        .Where(u => u.IsActive)
                        .Select(u => new LookupDto
                        {
                            Id = u.UserId,
                            Name = u.FullName,
                            FactoryId = u.FactoryId
                        })
                        .ToList();

                    _logger.LogInformation("✅ تم العثور على {Count} سائقين", drivers.Count);
                    return drivers;
                }

                _logger.LogWarning("⚠️ لم يتم العثور على سائقين.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب السائقين");
            }

            return new List<LookupDto>();
        }

        /// <summary>
        /// جلب أنواع الخرسانة
        /// </summary>
        public async Task<List<LookupDto>> GetConcreteTypesAsync()
        {
            try
            {
                _logger.LogInformation("🔍 جلب أنواع الخرسانة...");

                var response = await _apiClient.GetAsync<ApiResponse<List<ConcreteTypeDto>>>("ConcreteTypes");

                if (response != null && response.Success && response.Data != null)
                {
                    var types = response.Data
                        .Select(x => new LookupDto
                        {
                            Id = x.ConcreteTypeId,
                            Name = x.Name
,UnitPrice = x.UnitPrice
                        })
                        .ToList();

                    _logger.LogInformation("✅ تم العثور على {Count} أنواع خرسانة", types.Count);
                    return types;
                }

                _logger.LogWarning("⚠️ لم يتم العثور على أنواع الخرسانة.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب أنواع الخرسانة");
            }

            return new List<LookupDto>();
        }
    }
}