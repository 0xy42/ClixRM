using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Models;
using ClixRM.Services.Authentication;
using ClixRM.Services.Output;

namespace ClixRM.Commands.Auth;

public class ShowActiveCommand : Command
{
    private readonly ISecureStorage _storage;
    private readonly IOutputManager _outputManager;

    public ShowActiveCommand(ISecureStorage secureStorage, IOutputManager outputManager) : base("show-active", "Show the currently active connection.")
    {
        _storage = secureStorage;
        _outputManager = outputManager;

        this.SetHandler(HandleShowActive);
    }

    private void HandleShowActive()
    {
        try
        {
            var activeConnectionIdentifier = _storage.GetActiveConnectionIdentifier();
            if (activeConnectionIdentifier == null)
            {
                _outputManager.PrintWarning("Currently, no active connection is set. Use the 'auth' command to create a new connection.");
                return;
            }

            var activeConnection = _storage.GetConnection(activeConnectionIdentifier.EnvironmentName);

            var unsecureOutput = new ConnectionDetailsUnsecure(
                activeConnection.EnvironmentName,
                activeConnection.Url,
                activeConnection.ConnectionType,
                activeConnection.ClientId.ToString());

            _outputManager.PrintSuccess(FormatConnectionLine("Active", unsecureOutput));
        }
        catch (Exception ex)
        {
            _outputManager.PrintError($"An unexpected error occurred: {ex.Message}");
        }
    }

    private string FormatConnectionLine(string status, ConnectionDetailsUnsecure connection)
    {
        return string.Format("{0,-10} {1,-25} {2,-25} {3}",
            status,
            connection.EnvironmentName,
            connection.ConnectionType,
            connection.Identifier);
    }
}
