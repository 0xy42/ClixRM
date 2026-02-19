using ClixRM.Services.Output;
using ClixRM.Services.Security;
using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.CommandLine.Parsing;
using ClixRM.Sdk.Commands;
using ClixRM.Sdk.Services;
using ClixRM.Models;

namespace ClixRM.Commands.Security;

public class PrivilegeCheckCommand : CrmConnectedCommand<List<PrivilegeCheckResult>>
{
    private readonly ISecurityRoleAnalyzer _privilegeChecker;
    private readonly IOutputManager _outputManager;

    public PrivilegeCheckCommand(
        ISecurityRoleAnalyzer privilegeChecker, 
        IConfiguration configuration, 
        IOutputManager outputManager, 
        IActiveConnectionGuard activeConnectionGuard,
        ICommandResultFormatter<List<PrivilegeCheckResult>> formatter)
        : base("privilege-check", "Check how a specific privilege is granted to a user (directly or via teams).", activeConnectionGuard, formatter)
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

            Formatter.Format(results);
        }
        catch (Exception ex)
        {
            _outputManager.PrintError($"An error occurred during privilege check: {ex.Message}");
        }
    }
}