using ClixRM.Models;
using ClixRM.Sdk.Services;
using ClixRM.Services.Output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Formatter;

public class UserWithRoleCommandFormatter : ICommandResultFormatter<List<UserWithRoleResult>>
{
    private readonly IOutputManager _outputManager;

    public UserWithRoleCommandFormatter(IOutputManager outputManager)
    {
        _outputManager = outputManager; 
    }

    public void Format(List<UserWithRoleResult> results)
    {
        if (results.Count == 0)
        {
            _outputManager.PrintWarning("Found no users assigned this security role.");
            return;
        }

        _outputManager.PrintSuccess($"Found {results.Count} assignments for the security role.");

        var groupedResults = results.GroupBy(r => r.GrantType).OrderBy(g => g.Key);

        foreach (var group in groupedResults)
        {
            _outputManager.PrintInfo($"\n--- Assigned via: {group.Key} ---");
            var orderedGroup = group.OrderBy(r => r.UserName).ThenBy(r => r.TeamName);

            foreach (var result in orderedGroup)
            {
                if (result.GrantType == "Direct")
                {
                    _outputManager.PrintInfo($"- User: \"{result.UserName}\" ({result.UserId})");
                }
                else
                {
                    _outputManager.PrintInfo(
                        $"- User: \"{result.UserName}\" ({result.UserId}) | Team: \"{result.TeamName}\" ({result.TeamId})"
                    );
                }
            }
        }
    }
}
