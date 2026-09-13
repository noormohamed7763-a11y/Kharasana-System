using Kharasana.Application.DTOs.Auth;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kharasana.API.Controllers;

/// <summary>
/// تسجيل الدخول وإنشاء الحسابات — نقطة عامة (لا تتطلّب توكن).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // لا يحتاج توكن للوصول إلى التسجيل والدخول
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// تسجيل مستخدم جديد (دور Client فقط).
    /// </summary>
    /// <param name="dto">بيانات التسجيل: الاسم، الهاتف، كلمة المرور... إلخ.</param>
    /// <response code="201">تم إنشاء الحساب بنجاح.</response>
    /// <response code="400">بيانات غير صالحة أو الحساب موجود مسبقاً.</response>
    /// <response code="429">تجاوز حد المحاولات المسموح في الدقيقة.</response>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// تسجيل الدخول — يعيد توكن JWT يُستخدم بعدها في النقاط المحمية.
    /// </summary>
    /// <remarks>محمية بحد أقصى 10 محاولات في الدقيقة لكل عنوان IP.</remarks>
    /// <param name="dto">البريد الإلكتروني وكلمة المرور.</param>
    /// <response code="200">تم تسجيل الدخول — يرجع التوكن وبيانات المستخدم.</response>
    /// <response code="400">بريد إلكتروني أو كلمة مرور غير صحيحة.</response>
    /// <response code="429">تجاوز حد المحاولات المسموح في الدقيقة.</response>
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(result);
    }
}