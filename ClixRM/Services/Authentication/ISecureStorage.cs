using ClixRM.Models;

namespace ClixRM.Services.Authentication;

public interface ISecureStorage
{
    void SaveConnection(ConnectionDetails connection);
    ConnectionDetails GetConnection(string environment);
    IEnumerable<ConnectionDetailsUnsecure> ListConnectionsUnsecure();

    void RemoveConnection(string environment);
    void RemoveAllConnections();
    void SetActiveEnvironment(string environmentName);
    ActiveConnectionIdentifier? GetActiveConnectionIdentifier();

    IDictionary<string, ConnectionDetails> LoadAllConnections();
}