using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Models
{
    public record PrivilegeCheckResult(
        string GrantType,
        string RoleName,
        Guid RoleId,
        string PrivilegeName,
        string PrivilegeScope,
        string? TeamName,
        Guid? TeamId
    );

    public record SecurityRoleCheckResult(
        string GrantType,
        string RoleName,
        Guid RoleId,
        string? TeamName,
        Guid? TeamId
    );

    public record UserWithRoleResult(
        string GrantType,
        Guid UserId,
        string UserName,
        string? TeamName,
        Guid? TeamId
    );
}
