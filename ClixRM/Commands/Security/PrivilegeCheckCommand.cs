using ClixRM.Services.Output;
using ClixRM.Services.Security;
using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace ClixRM.Commands.Security;

public class PrivilegeCheckCommand : CrmConnectedCommand
{
    private readonly ISecurityRoleAnalyzer _privilegeChecker;
    private readonly IOutputManager _outputManager;

    public PrivilegeCheckCommand(ISecurityRoleAnalyzer privilegeChecker, IConfiguration configuration, IOutputManager outputManager)
        : base("privilege-check", "Check how a specific privilege is granted to a user (directly or via teams).")
    {
        _privilegeChecker = privilegeChecker;
        _outputManager = outputManager;

        var userIdOption = CreateUserIdOption();
        var privilegeOption = CreatePrivilegeOption();

        AddOption(userIdOption);
        AddOption(privilegeOption);

        this.SetHandler(async (userId, privilege) =>
        {
            await HandleCommandAsync(userId, privilege);
        }, userIdOption, privilegeOption);
    }

    private static Option<string> CreateUserIdOption()
    {
        var userIdOption = new Option<string>(
            aliases: ["--user-id", "-u"],
            description: "The GUID of the user to check privileges for.")
        {
            IsRequired = true,
            ArgumentHelpName = "guid"
        };
        userIdOption.AddValidator(ValidateGuid);
        return userIdOption;
    }

    private static Option<string> CreatePrivilegeOption()
    {
        return new Option<string>(
            aliases: ["--privilege", "-p"],
            description: "The logical name of the privilege to check (e.g., 'prvCreateAccount').")
        {
            IsRequired = true,
            ArgumentHelpName = "privName"
        };
    }

    private static void ValidateGuid(OptionResult optionResult)
    {
        var value = optionResult.GetValueOrDefault<string>();
        if (!Guid.TryParse(value, out _))
        {
            optionResult.ErrorMessage = "The --user-id must be a valid GUID.";
        }
    }

    /// <summary>
    ///     Handles the asynchronous execution of the command, retrieving and formatting privilege check results.
    /// </summary>
    private async Task HandleCommandAsync(string userIdString, string privilegeName)
    {
        var userId = Guid.Parse(userIdString);

        _outputManager.PrintInfo($"Checking how privilege '{privilegeName}' is granted to user '{userId}'...");

        try
        {
            var results = await _privilegeChecker.CheckPrivilegeAsync(userId, privilegeName);

            if (results.Count == 0)
            {
                _outputManager.PrintWarning($"Privilege '{privilegeName}' is either not found in the system or not granted to user '{userId}' directly or via teams.");
            }
            else
            {
                _outputManager.PrintSuccess($"Found {results.Count} grant path(s) for privilege '{privilegeName}':");

                var groupedResults = results.GroupBy(r => r.GrantType).OrderBy(g => g.Key);

                foreach (var group in groupedResults)
                {
                    _outputManager.PrintInfo($"\n--- Granted via: {group.Key} ---");
                    var orderedGroup = group.OrderBy(r => r.RoleName).ThenBy(r => r.TeamName);

                    foreach (var result in orderedGroup)
                    {
                        _outputManager.PrintInfo(
                            result.GrantType == "Direct"
                                ? $"- Role: \"{result.RoleName}\" ({result.RoleId}) | Scope: {result.PrivilegeScope}"
                                : $"- Role: \"{result.RoleName}\" ({result.RoleId}) | Team: \"{result.TeamName}\" ({result.TeamId}) | Scope: {result.PrivilegeScope}"
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _outputManager.PrintError($"An error occurred during privilege check: {ex.Message}");
        }
    }
}