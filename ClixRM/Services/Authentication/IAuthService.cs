using ClixRM.Models;

namespace ClixRM.Services.Authentication
{
    public interface IAuthService
    {
        Task<AppSecretConnectionDetails> AuthenticateAppAsync(
            Guid clientId,
            string clientSecret,
            string url,
            string connectionName
        );

        Task<UserConnectionDetails> AuthenticateWithUserAsync(
            string url, string connectionName);
    }
}
