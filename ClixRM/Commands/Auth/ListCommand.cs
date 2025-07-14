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
            var connections = _storage.ListConnectionsUnsecure();

            AppRegistrationConnectionDetailsUnsecure? activeConnection = null;
            var activeConnectionIdentifier = _storage.GetActiveConnectionIdentifier();
            if (activeConnectionIdentifier != null)
            {
                activeConnection = connections.FirstOrDefault(c => c.ConnectionId == activeConnectionIdentifier.ConnectionId);
                if (activeConnection == null)
                {
                    _outputManager.PrintWarning("Found an active connection identifiert, but unable to retrieve active connection. Consider clearing your authentications.");
                }
                else 
                {
                    _outputManager.PrintSuccess("(ACTIVE) " + activeConnection.ToString());
                }
            }

            foreach (var connection in connections.Where(c => c.ConnectionId != activeConnection?.ConnectionId))
            {
                _outputManager.PrintInfo("         " +connection.ToString());
            }
        }
        catch(Exception ex)
        {
            _outputManager.PrintError($"An unexpected error occurred: {ex.Message}");
        }
    }
}
