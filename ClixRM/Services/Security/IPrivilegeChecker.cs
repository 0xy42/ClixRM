namespace ClixRM.Services.Security;

public interface IPrivilegeChecker
{
    public Task<List<PrivilegeCheckResult>> CheckPrivilegeAsync(Guid userId, string privilegeName);
}