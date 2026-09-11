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
}