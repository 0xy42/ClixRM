using ClixRM.Models;

namespace ClixRM.Services.Authentication
{
    public interface IAuthService
    {
        Task<AppRegistrationConnectionDetailsSecure> AuthenticateAsync(
            Guid clientId,
            string clientSecret,
            Guid tenantId,
            string url,
            string connectionName
        );

        Task<AppRegistrationConnectionDetailsSecure> AuthenticateAsync(
            Guid clientId,
            string clientSecret,
            string url,
            string connectionName
        );
    }
}
