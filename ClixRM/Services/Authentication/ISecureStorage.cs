using ClixRM.Models;

namespace ClixRM.Services.Authentication;

public interface ISecureStorage
{
    void SaveConnection(AppRegistrationConnectionDetails connection);
    AppRegistrationConnectionDetails GetConnection(string environment);
    void RemoveConnection(string environment);
    void RemoveAllConnections();
    IDictionary<string, AppRegistrationConnectionDetails> LoadAllConnections();

    // Modified methods
    void SetActiveEnvironment(string environmentName);
    ActiveConnectionIdentifier? GetActiveConnectionIdentifier();
}