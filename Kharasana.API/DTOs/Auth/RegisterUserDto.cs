using System.ComponentModel.DataAnnotations;
using Kharasana.Application.Common;
using Kharasana.Domain.Validation;

namespace Kharasana.API.DTOs.Auth;

public class RegisterUserDto
{
    [Required(ErrorMessage = Messages.FullNameRequired)]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [YemeniEmail(ErrorMessage = Messages.InvalidEmail)]
    public string? Email { get; set; }

    [Required(ErrorMessage = Messages.PasswordRequired)]
    [MinLength(PasswordPolicy.MinimumLength, ErrorMessage = Messages.PasswordMinLength)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = Messages.ConfirmPasswordRequired)]
    [Compare(nameof(Password), ErrorMessage = Messages.PasswordsNotMatch)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? WhatsApp { get; set; }
}