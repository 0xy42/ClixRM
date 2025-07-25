using ClixRM.Models;
using ClixRM.Sdk.Models;

namespace ClixRM.Services.Authentication;

/// <summary>
///     Secure storage mechanism for ClixRM using Windows DPAPI.
/// </summary>
public interface ISecureStorage
{
    /// <summary>
    ///     Save a newly established connection to the secure storage.
    /// </summary>
    /// <param name="connection">The connection to store.</param>
    void SaveConnection(ConnectionDetails connection);
    
    /// <summary>
    ///     Get a stored connection by its name identifier from the secure storage.
    /// </summary>
    /// <param name="name">The user-friendly name set for the connection.</param>
    /// <returns>The connection's <see cref="ConnectionDetails"/>.</returns>
    ConnectionDetails GetConnection(string name);
    
    /// <summary>
    ///     List the unsecure connection details of all stored connections, sensitive information omitted.
    /// </summary>
    /// <returns>The <see cref="IEnumerable{T}"/> collection of <see cref="ConnectionDetailsUnsecure"/>.</returns>
    IEnumerable<ConnectionDetailsUnsecure> ListConnectionsUnsecure();

    /// <summary>
    ///     Remove a connection with the provided identifier name.
    /// </summary>
    /// <param name="name">The connection to remove.</param>
    void RemoveConnection(string name);
    
    /// <summary>
    ///     Removes all stored connections.
    /// </summary>
    void RemoveAllConnections();
    
    /// <summary>
    ///     Sets an environment as active connection by its identifier name.
    /// </summary>
    /// <param name="environmentName">The environment to set as active.</param>
    void SetActiveEnvironment(string environmentName);
    
    /// <summary>
    ///     Get the <see cref="ActiveConnectionIdentifier"/>
    /// </summary>
    /// <returns>The currently set <see cref="ActiveConnectionIdentifier"/>.</returns>
    ActiveConnectionIdentifier? GetActiveConnectionIdentifier();

    /// <summary>
    ///     Load all connections from the DPAPI secure storage.
    /// </summary>
    /// <returns>All connection's details.</returns>
    IDictionary<string, ConnectionDetails> LoadAllConnections();
    
    /// <summary>
    ///     Helper method to validate if an active connection exists.
    /// </summary>
    /// <returns>True if an active connection exists.</returns>
    bool DoesActiveConnectionExist();
}