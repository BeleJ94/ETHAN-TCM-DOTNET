using System.ComponentModel.DataAnnotations;
using EthanTcm.Application.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EthanTcm.Web.Models;

public sealed class AccessUsersViewModel { public AccessUserPage Page { get; init; } = null!; public string? Search { get; init; } public bool? Active { get; init; } }
public sealed class AccessUserEditViewModel
{
    [ValidateNever]
    public AccessUserDetails User { get; init; } = null!;
    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
    public Guid[] RoleIds { get; set; } = [];
    [Required, RegularExpression("^(en|fr)$")] public string PreferredCulture { get; set; } = "en";
    [Required, StringLength(500)] public string Reason { get; set; } = string.Empty;
}
public sealed class AccessRolesViewModel { public IReadOnlyCollection<AccessRoleMatrixItem> Roles { get; init; } = []; }
public sealed class AccessRoleEditViewModel
{
    public Guid RoleId { get; set; }
    public Guid[] PermissionIds { get; set; } = [];
    [Required, StringLength(500)] public string Reason { get; set; } = string.Empty;
}
