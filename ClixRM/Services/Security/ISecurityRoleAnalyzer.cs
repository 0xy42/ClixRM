namespace ClixRM.Services.Security;

public interface ISecurityRoleAnalyzer
{
    public Task<List<PrivilegeCheckResult>> CheckPrivilegeAsync(Guid userId, string privilegeName);
    public Task<List<SecurityRoleCheckResult>> CheckSecurityRolesAsync(Guid userId);
}