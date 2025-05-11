using ClixRM.Services.Authentication;
using System.CommandLine;

namespace ClixRM.Commands.Auth;

public class SwitchEnvironmentCommand : Command
{
    private readonly ISecureStorage _secureStorage;

    public SwitchEnvironmentCommand(ISecureStorage secureStorage)
        : base("switch", "Switch to a different environment")
    {
        _secureStorage = secureStorage;

        var environmentArgument = CreateEnvironmentArgument();

        AddArgument(environmentArgument);

        this.SetHandler(HandleSwitch, environmentArgument);
    }

    private static Argument<string> CreateEnvironmentArgument()
    {
        return new Argument<string>("environment", "The environment to switch to");
    }

    private void HandleSwitch(string environment)
    {
        try
        {
            _ = _secureStorage.GetConnection(environment.ToLower());

            _secureStorage.SetActiveEnvironment(environment.ToLower());

            Console.WriteLine($"Switched to environment '{environment}'. All subsequent commands will use this connection.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}