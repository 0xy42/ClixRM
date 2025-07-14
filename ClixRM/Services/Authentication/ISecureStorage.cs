using ClixRM.Models;

namespace ClixRM.Services.Authentication;

public interface ISecureStorage
{
    void SaveConnection(AppRegistrationConnectionDetailsSecure connection);
    AppRegistrationConnectionDetailsSecure GetConnection(string environment);
    IEnumerable<AppRegistrationConnectionDetailsUnsecure> ListConnectionsUnsecure();
    void RemoveConnection(string environment);
    void RemoveAllConnections();
    IDictionary<string, AppRegistrationConnectionDetailsSecure> LoadAllConnections();

    // Modified methods
    void SetActiveEnvironment(string environmentName);
    ActiveConnectionIdentifier? GetActiveConnectionIdentifier();
}