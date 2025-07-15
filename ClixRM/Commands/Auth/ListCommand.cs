using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Models;
using ClixRM.Services.Authentication;
using ClixRM.Services.Output;
using Newtonsoft.Json;

namespace ClixRM.Commands.Auth;

public class ListCommand : Command
{
    private readonly ISecureStorage _storage;
    private readonly IOutputManager _outputManager;

    public ListCommand(ISecureStorage secureStorage, IOutputManager outputManager) : base("list", "List the existing connections.")
    {
        _storage = secureStorage;
        _outputManager = outputManager;

        this.SetHandler(HandleList);
    }

    private void HandleList()
    {
        try
        {
            var connections = _storage.ListConnectionsUnsecure().ToList();

            if (!connections.Any())
            {
                _outputManager.PrintInfo("No connections have been saved yet.");
                return;
            }

            var activeConnectionIdentifier = _storage.GetActiveConnectionIdentifier();
            string? activeConnectionName = activeConnectionIdentifier?.EnvironmentName;

            _outputManager.PrintInfo(string.Format("{0,-10} {1,-25} {2,-25} {3}", "Status", "Name", "Type", "Identifier"));
            _outputManager.PrintInfo(new string('-', 70));

            if (activeConnectionName != null)
            {
                var activeConnection = connections.FirstOrDefault(c => c.EnvironmentName.Equals(activeConnectionName, StringComparison.OrdinalIgnoreCase));
                if (activeConnection != null)
                {
                    _outputManager.PrintSuccess(FormatConnectionLine(" (Active)", activeConnection));
                }
                else
                {
                    _outputManager.PrintWarning("Found an active connection identifier, but unable to retrieve the connection. Consider clearing your authentications.");
                }
            }

            foreach (var connection in connections.Where(c => !c.EnvironmentName.Equals(activeConnectionName, StringComparison.OrdinalIgnoreCase)))
            {
                _outputManager.PrintInfo(FormatConnectionLine("", connection));
            }
        }
        catch(Exception ex)
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
