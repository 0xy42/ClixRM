using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Models;
using ClixRM.Services.Output;
using ClixRM.Services.Security;

namespace ClixRM.Commands.Security;

public class UsersWithRoleCommand : CrmConnectedCommand
{
    private readonly ISecurityRoleAnalyzer _analyzer;
    private readonly IOutputManager _outputManager;

    public UsersWithRoleCommand(ISecurityRoleAnalyzer analyzer, IOutputManager outputManager) 
        : base ("users-with-role", "List all users of an environment that are assigned a specific security role (directly or via teams).")
    {
        _analyzer = analyzer;
        _outputManager = outputManager;

        var roleIdOption = CreateRoleIdOption();
        var roleNameOption = CreateRoleNameOption();

        AddOption(roleIdOption);
        AddOption(roleNameOption);

        this.AddValidator(result => ValidateMutuallyExclusiveFlags(result, roleIdOption, roleNameOption));

        this.SetHandler(HandleUsersWithRole, roleIdOption, roleNameOption);
    }

    private static void ValidateMutuallyExclusiveFlags(CommandResult commandResult, Option roleIdOption, Option roleNameOption)
    {
        var roleIdResult = commandResult.FindResultFor(roleIdOption);
        var roleNameResult = commandResult.FindResultFor(roleNameOption);

        if (roleIdResult is not null && roleNameResult is not null)
        {
            commandResult.ErrorMessage = $"Cannot use `{roleIdOption.Name}` and `{roleNameOption.Name}` at the same time. Please provide only one.";
        }
        else if (roleIdResult is null && roleNameResult is null)
        {
            commandResult.ErrorMessage = $"You must provide either `{roleIdOption.Name}` or `{roleNameOption.Name}`.";
        }
    } 

    private static Option<Guid> CreateRoleIdOption()
    {
        return new Option<Guid>(["--role-id", "-r"], "The GUID of the security role to analyze.")
        {
            ArgumentHelpName = "guid"
        };
    }

    private static Option<string> CreateRoleNameOption()
    {
        return new Option<string>(["--name", "-n"], "The name of the security role to analyze.")
        {
            ArgumentHelpName = "roleName"
        };
    }

    private async Task HandleUsersWithRole(Guid roleId, string roleName)
    {
        try
        {
            List<UserWithRoleResult> results;

            if (!string.IsNullOrEmpty(roleName))
            {
                results = await _analyzer.GetUsersWithRoleAsync(roleName);
            }
            else
            {
                results = await _analyzer.GetUsersWithRoleAsync(roleId);
            }

            if (results.Count == 0)
            {
                _outputManager.PrintWarning($"Found no users assigned security role '{roleId}.'");
                return;
            }

            _outputManager.PrintSuccess($"Found {results.Count} assignments for security role '{roleId}'.");

            var groupedResults = results.GroupBy(r => r.GrantType).OrderBy(g => g.Key);

            foreach (var group in groupedResults)
            {
                _outputManager.PrintInfo($"\n--- Assigned via: {group.Key} ---");
                var orderedGroup = group.OrderBy(r => r.UserName).ThenBy(r => r.TeamName);

                foreach (var result in orderedGroup)
                {
                    _outputManager.PrintInfo(
                        result.GrantType == "Direct"
                            ? $"- User: \"{result.UserName}\" ({result.UserId})"
                            : $"- User: \"{result.UserName}\" ({result.UserId}) | Team: \"{result.TeamName}\" ({result.TeamId})"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _outputManager.PrintError($"An error occurred during analysis: {ex.Message}.");
        }
    }
}
