using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Services.Output;
using ClixRM.Services.Security;

namespace ClixRM.Commands.Security
{
    public class ListUserRolesCommand : CrmConnectedCommand
    {
        private readonly IOutputManager _outputManager;
        private readonly ISecurityRoleAnalyzer _securityRoleAnalyzer;

        public ListUserRolesCommand(IOutputManager outputManager, ISecurityRoleAnalyzer securityRoleAnalyzer)
            : base("list-user-roles", "List all security roles assigned to a specific user (directly or via teams).")
        {
            _outputManager = outputManager;
            _securityRoleAnalyzer = securityRoleAnalyzer;

            var userIdOption = CreateUserIdOption();

            AddOption(userIdOption);
           
            this.SetHandler(HandleCommandAsync, userIdOption);
        }

        private static Option<string> CreateUserIdOption()
        {
            var userIdOption = new Option<string>(
                aliases: ["--user-id", "-u"],
                description: "The GUID of the user to check security roles for.")
            {
                IsRequired = true,
                ArgumentHelpName = "guid"
            };
            userIdOption.AddValidator(ValidateGuid);

            return userIdOption;
        }

        private static void ValidateGuid(OptionResult optionResult)
        {
            var value = optionResult.GetValueOrDefault<string>();
            if (!Guid.TryParse(value, out _))
            {
                optionResult.ErrorMessage = "The --user-id must be a valid GUID.";
            }
        }

        private async Task HandleCommandAsync(string userIdstring)
        {
            var userId = Guid.Parse(userIdstring);

            _outputManager.PrintInfo($"Checking security roles for user '{userId}'...");

            try
            {
                var results = await _securityRoleAnalyzer.CheckSecurityRolesAsync(userId);

                if (results.Count == 0)
                {
                    _outputManager.PrintWarning($"Found no security roles assigned to user '{userId}'");
                }
                else
                {
                    _outputManager.PrintSuccess($"Found {results.Count} security roles for user '{userId}'");

                    var groupedResults = results.GroupBy(r => r.GrantType).OrderBy(g => g.Key);

                    foreach(var group in groupedResults)
                    {
                        _outputManager.PrintInfo($"\n--- Granted via: {group.Key} ---");
                        var orderedGroup = group.OrderBy(r => r.RoleName).ThenBy(r => r.TeamName);

                        foreach (var result in orderedGroup)
                        {
                            _outputManager.PrintInfo(
                            result.GrantType == "Direct"
                                ? $"- Role: \"{result.RoleName}\" ({result.RoleId})"
                                : $"- Role: \"{result.RoleName}\" ({result.RoleId}) | Team: \"{result.TeamName}\" ({result.TeamId})"
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _outputManager.PrintError($"An error occured during security role check: {ex.Message}");
            }
        }
    }
}
