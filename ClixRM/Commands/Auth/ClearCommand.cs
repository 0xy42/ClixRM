using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Sdk.Services;
using ClixRM.Services.Authentication;
using ClixRM.Services.Output;

namespace ClixRM.Commands.Auth;

public class ClearCommand : Command
{
    private readonly ISecureStorage _storage;
    private readonly IOutputManager _outputManager;

    public ClearCommand(ISecureStorage storage, IOutputManager outputManager) : base("clear", "Clear and remove all stored connections.")
    {
        _storage = storage;
        _outputManager = outputManager;

        this.SetHandler(HandleClear);
    }

    private void HandleClear()
    {
        try
        {
            _storage.RemoveAllConnections();
            _outputManager.PrintSuccess("Cleared all existing connections.");
        }
        catch(Exception ex)
        {
            _outputManager.PrintError($"An unexpected error occurred: {ex.Message}");
        }
    }
}
