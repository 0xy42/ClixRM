using ClixRM.Models;

namespace ClixRM.Services.Authentication
{
    public interface IAuthService
    {
        Task<AppRegistrationConnectionDetails> AuthenticateAsync(
            string clientId,
            string clientSecret,
            string tenantId,
            string url,
            string connectionName
        );
    }
}
