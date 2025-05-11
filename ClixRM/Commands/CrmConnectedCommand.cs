using ClixRM.Services.Authentication;
using System.CommandLine;
using System.CommandLine.Parsing;


namespace ClixRM.Commands;

public class CrmConnectedCommand : Command
{
    protected CrmConnectedCommand(string name, string description)
        : base(name, description)
    {
        AddValidator(ValidateActiveConnection);
    }

    protected void ValidateActiveConnection(CommandResult result)
    {
        var activeEnvironment = SecureStorage.DoesActiveConnectionExist();

        if (!activeEnvironment)
        {
            result.ErrorMessage = "No active connection found. Please log in and switch to an environment first.";
        }
    }
}