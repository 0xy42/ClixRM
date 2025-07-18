using ClixRM.Models;

namespace ClixRM.Services.Security;

public interface ISecurityRoleAnalyzer
{
    public Task<List<PrivilegeCheckResult>> CheckPrivilegeAsync(Guid userId, string privilegeName);
    public Task<List<SecurityRoleCheckResult>> CheckSecurityRolesAsync(Guid userId);
    public Task<List<UserWithRoleResult>> GetUsersWithRoleAsync(Guid roleId);
    public Task<List<UserWithRoleResult>> GetUsersWithRoleAsync(string roleName);
}