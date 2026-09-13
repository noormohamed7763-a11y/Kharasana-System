using Kharasana.Application.Common;
using Kharasana.Web.Models.Users;

namespace Kharasana.Web.ViewModels.Users;

public class UsersIndexViewModel
{
    public PagedResult<UserListItemViewModel>? PagedUsers { get; set; }

    public string? Search { get; set; }

    public string? Role { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    /// <summary>إجمالي المستخدمين (فعلي عبر كل الصفحات).</summary>
    public int TotalUsers { get; set; }

    /// <summary>عدد مديري النظام (فعلي عبر كل الصفحات).</summary>
    public int AdminsCount { get; set; }

    /// <summary>عدد موظفي المصانع (فعلي عبر كل الصفحات).</summary>
    public int FactoryEmployeesCount { get; set; }

    /// <summary>عدد السائقين (فعلي عبر كل الصفحات).</summary>
    public int DriversCount { get; set; }
}