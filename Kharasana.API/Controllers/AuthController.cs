using Kharasana.Application.DTOs.Auth;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kharasana.API.Controllers;

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
    /// تسجيل مستخدم جديد (Client فقط)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// تسجيل الدخول — محمي بحد أقصى 10 محاولات في الدقيقة لكل IP
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(result);
    }
}