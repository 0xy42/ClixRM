using System.CommandLine;
using System.CommandLine.Parsing;
using ClixRM.Sdk.Services;

namespace ClixRM.Sdk.Commands;

public class CrmConnectedCommand : Command
{
    private readonly IActiveConnectionGuard _activeConnectionGuard;
    protected CrmConnectedCommand(string name, string description,  IActiveConnectionGuard activeConnectionGuard)
        : base(name, description)
    {
        _activeConnectionGuard = activeConnectionGuard;
        AddValidator(ValidateActiveConnection);
    }

    protected void ValidateActiveConnection(CommandResult result)
    {
        var activeEnvironment = _activeConnectionGuard.DoesActiveConnectionExist();

        if (!activeEnvironment)
        {
            result.ErrorMessage = "No active connection found. Please log in and switch to an environment first.";
        }
    }
}