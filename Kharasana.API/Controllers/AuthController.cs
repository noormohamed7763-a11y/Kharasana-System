using Asp.Versioning;
using Kharasana.API.Common;
using Kharasana.Application.DTOs.Auth;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kharasana.API.Controllers;

/// <summary>
/// تسجيل الدخول وإنشاء الحسابات — نقطة عامة.
/// </summary>
/// <remarks>
/// هذا المتحكم محمي بـ <c>ValidationFilter</c> للتحقق من بيانات الطلبات.
/// يتم الاعتماد على الـ DTOs الخاصة بطبقة التطبيق (Application) مباشرة لضمان عمل الفلتر
/// بشكل صحيح مع الفاحصات (Validators) المسجلة في النظام.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
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
    /// <remarks>
    /// محمية بنفس سياسة الدخول «login»: 10 محاولات في الدقيقة لكل عنوان IP.
    /// إعادة استخدام السياسة مقصودة لا سهو — التسجيل نقطة مجهولة بلا توكن، وردّها
    /// 409 (بريد/هاتف مسجَّل) مقابل 201 يكشف للمجهول ما هو مسجَّل في النظام،
    /// فحاجزها يجب ألا يقلّ عن حاجز الدخول. الحاجز العام (100/دقيقة) وحده أوسع من أن يمنعه.
    /// </remarks>
    /// <param name="dto">بيانات التسجيل: الاسم، الهاتف، كلمة المرور... إلخ.</param>
    /// <response code="201">تم إنشاء الحساب بنجاح.</response>
    /// <response code="400">بيانات غير صالحة (كلمة المرور، الهاتف، البريد... إلخ).</response>
    /// <response code="409">الحساب موجود مسبقاً — بريد إلكتروني أو رقم هاتف مسجّل.</response>
    /// <response code="429">تجاوز حد المحاولات المسموح في الدقيقة.</response>
    [HttpPost("register")]
    [EnableRateLimiting("login")]
    [ServiceFilter(typeof(ValidationFilter<RegisterUserDto>))]
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
    [ServiceFilter(typeof(ValidationFilter<LoginRequestDto>))]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(result);
    }

    [HttpPost("activate")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Activate([FromBody] string tokenHash)
    {
        var result = await _authService.ActivateAccountAsync(tokenHash);
        return Ok(result);
    }
}
