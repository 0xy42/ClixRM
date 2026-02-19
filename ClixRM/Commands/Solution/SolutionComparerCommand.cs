using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Models.Solutions;
using ClixRM.Sdk.Commands;
using ClixRM.Sdk.Services;
using ClixRM.Services.Solutions;
using Microsoft.Extensions.Logging;

namespace ClixRM.Commands.Solution;

public class SolutionComparerCommand : CrmConnectedCommand<SolutionComparisonResult>
{
    private readonly ISolutionComparer _solutionComparer;
    private readonly IOutputManager _outputManager;
    private readonly ILogger<SolutionComparerCommand> _logger;

    public SolutionComparerCommand(
        ISolutionComparer solutionComparer,
        IOutputManager outputManager,
        ICommandResultFormatter<SolutionComparisonResult> formatter,
        ILogger<SolutionComparerCommand> logger,
        IActiveConnectionGuard activeConnectionGuard)
        : base("compare", "Compare two solution sets to identify differences and similarities.", activeConnectionGuard, formatter)
    {
        _solutionComparer = solutionComparer;
        _outputManager = outputManager;
        _logger = logger;

        var environment1Option = CreateEnv1Option();
        var environment2Option = CreateEnv2Option();

        var solutions1Option = CreateSolutions1Option();
        var solutions2Option = CreateSolutions2Option();

        AddOption(environment1Option);
        AddOption(environment2Option);
        AddOption(solutions1Option);
        AddOption(solutions2Option);

        this.SetHandler(HandleSolutionComparison, environment1Option, solutions1Option, environment2Option, solutions2Option);
    }

    private static Option<string?> CreateEnv1Option()
    {
        return new Option<string?>(
            aliases: ["--env1", "-e1"],
            description: "Environment name for the first solution set (optional, uses active connection if not specified)")
        {
            IsRequired = false,
        };
    }

    private static Option<string?> CreateEnv2Option()
    {
        return new Option<string?>(
            aliases: ["--env2", "-e2"],
            description: "Environment name for the second solution set (optional, uses active connection if not specified)")
        {
            IsRequired = false,
        };
    }

    private static Option<string[]> CreateSolutions1Option()
    {
        return new Option<string[]>(
            aliases: ["--solutions1", "-s1"],
            description: "The first set of solutions to compare, specified as comma separated list.",
            parseArgument: result =>
            {
                var value = result.Tokens.Single().Value;
                return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            })
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };
    }

    private static Option<string[]> CreateSolutions2Option()
    {
        return new Option<string[]>(
            aliases: ["--solutions2", "-s2"],
            description: "The second set of solutions to compare, specified as comma separated list.",
            parseArgument: result =>
            {
                var value = result.Tokens.Single().Value;
                return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            })
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };
    }

    private async Task HandleSolutionComparison(string? env1, string[] solutions1, string? env2, string[] solutions2)
    {
        if ((env1 != null && env2 == null) || (env1 == null && env2 != null))
        {
            _outputManager.PrintError("Both --env1 and --env2 must be specified together, or neither should be specified to use the active connection.");
            return;
        }

        if (env1 != null && env2 != null)
        {
            _outputManager.PrintInfo($"Environment 1: {env1}");
            _outputManager.PrintInfo($"Set 1: {string.Join(", ", solutions1)}");
            _outputManager.PrintInfo($"Environment 2: {env2}");
            _outputManager.PrintInfo($"Set 2: {string.Join(", ", solutions2)}");
        }
        else
        {
            _outputManager.PrintInfo("Comparing solution sets...");
            _outputManager.PrintInfo($"Set 1: {string.Join(", ", solutions1)}");
            _outputManager.PrintInfo($"Set 2: {string.Join(", ", solutions2)}");
        }

        _outputManager.PrintInfo("");

        try
        {
            SolutionComparisonResult result;

            if (env1 != null && env2 != null)
            {
                result = await _solutionComparer.CompareSolutionsAsync(env1, solutions1, env2, solutions2);
            }
            else
            {
                result = await _solutionComparer.CompareSolutionsAsync(solutions1, solutions2);
            }

            Formatter.Format(result);
        }
        catch (Exception ex)
        {
            _outputManager.PrintError($"An error occurred during solution comparison: {ex.Message}");
        }
    }
}