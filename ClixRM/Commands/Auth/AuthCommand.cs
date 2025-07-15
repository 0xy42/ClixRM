using System.CommandLine;

namespace ClixRM.Commands.Auth;

public class AuthCommand : Command
{
    public AuthCommand(LoginAppCommand loginCommand, SwitchEnvironmentCommand switchEnvironmentCommand, ListCommand listCommand, ShowActiveCommand showActiveCommand)
        : base("auth", "Authentication commands for managing connections to environments")
    {
        AddCommand(loginCommand);
        AddCommand(switchEnvironmentCommand);
        AddCommand(listCommand);
        AddCommand(showActiveCommand);
    }
}