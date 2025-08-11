using System.CommandLine;
using ClixRM.FirstParty.Services.Flows;
using ClixRM.Sdk.Commands;
using ClixRM.Sdk.Services;

namespace ClixRM.FirstParty.Commands.Flows;

public class ColumnDependencyCheckCommand : SolutionAwareCommand
{
    private readonly FlowFieldDependencyAnalyzer _analyzer;
    private readonly IOutputManager _outputManager;
    private readonly ISolutionPathResolver _solutionPathResolver;

    public ColumnDependencyCheckCommand(
        IOutputManager outputManager,
        ISolutionPathResolver solutionPathResolver)
        : base("column-dependency",
               "Check all flows in a solution for dependencies on a specific entity field.")
    {
        _outputManager = outputManager;
        _solutionPathResolver = solutionPathResolver;

        var entityOption = CreateEntityOption();
        var columnOption = CreateColumnOption();
        var actionFilterOption = CreateActionFilterOption();
        var actionsOnlyOption = CreateActionsOnlyOption();
        var triggersOnlyOption = CreateTriggersOnlyOption();

        AddOption(entityOption);
        AddOption(columnOption);
        AddOption(actionFilterOption);
        AddOption(actionsOnlyOption);
        AddOption(triggersOnlyOption);

        _analyzer = new FlowFieldDependencyAnalyzer();

        this.SetHandler(
            HandleCommandAsync,
            entityOption, columnOption, actionFilterOption, actionsOnlyOption, triggersOnlyOption,
            SolutionAwareCommand.OnlineSolutionOption,
            SolutionAwareCommand.DirectoryOption,
            SolutionAwareCommand.ForceDownloadOption
        );
    }
    private static Option<string> CreateEntityOption()
    {
        var entityOption = new Option<string>(["--entity", "-e"], "The logical singular name of the entity to check...")
        { IsRequired = true, ArgumentHelpName = "entity" };
        return entityOption;
    }
    private static Option<string> CreateColumnOption()
    {
        var columnOption = new Option<string>(["--column", "-c"], "The logical name of the column to check for dependencies.")
        { IsRequired = true, ArgumentHelpName = "column" };
        return columnOption;
    }

    private static Option<string> CreateActionFilterOption()
    {
        var actionFilterOption = new Option<string>(["--action", "-a"], "The action filter to apply...")
        { IsRequired = false, ArgumentHelpName = "action" };
        return actionFilterOption;
    }

    private static Option<bool> CreateActionsOnlyOption()
    {
        var actionsOnlyOption = new Option<bool>(["--actions-only", "-ao"], "If set, only actions will be included...")
        { IsRequired = false };
        return actionsOnlyOption;
    }

    private static Option<bool> CreateTriggersOnlyOption()
    {
        var triggersOnlyOption = new Option<bool>(["--triggers-only", "-to"], "If set, only triggers will be included...")
        { IsRequired = false };
        return triggersOnlyOption;
    }

    private async Task HandleCommandAsync(
        string entityName, string columnName, string? actionFilter, bool actionsOnly, bool triggersOnly, // Command-specific
        string? onlineSolutionName, string? directoryPath, bool forceDownload) // Shared
    {
        if (actionsOnly && triggersOnly)
        {
            _outputManager.PrintError("Error: Cannot use both --actions-only and --triggers-only flags simultaneously.");
            return;
        }

        var actualSolutionPathToAnalyze = await _solutionPathResolver.ResolveSolutionPathAsync(onlineSolutionName, directoryPath, forceDownload);

        if (string.IsNullOrEmpty(actualSolutionPathToAnalyze))
        {
            return;
        }

        _outputManager.PrintInfo($"\nSearching for dependencies on field '{columnName}' of entity '{entityName}' in '{actualSolutionPathToAnalyze}'...");
        if (actionsOnly) _outputManager.PrintInfo("Scope: Actions only.");
        if (triggersOnly) _outputManager.PrintInfo("Scope: Triggers only.");
        if (!string.IsNullOrEmpty(actionFilter)) _outputManager.PrintInfo($"Action Filter: '{actionFilter}'.");

        try
        {
            var results = _analyzer.AnalyzeFieldUsage(actualSolutionPathToAnalyze, entityName, columnName, actionFilter, actionsOnly, triggersOnly);

            if (results.Count == 0)
            {
                _outputManager.PrintWarning($"No dependencies found for field '{columnName}' on entity '{entityName}'.");
            }
            else
            {
                _outputManager.PrintSuccess($"Found {results.Count} dependencies:");
                var groupedResults = results.GroupBy(r => r.FileName).OrderBy(g => g.Key);
                foreach (var group in groupedResults)
                {
                    _outputManager.PrintInfo($"\n--- Flow File: {group.Key} ---");
                    foreach (var result in group.OrderBy(r => r.SourceType).ThenBy(r => r.SourceName))
                    {
                        _outputManager.PrintInfo(
                            $"- {result.SourceType}: \"{result.SourceName}\" | Type: {result.DependencyType} | Entity: {result.EntityName} | Field: {result.FieldName} | Details: {result.Details}"
                        );
                    }
                }
            }
        }
        catch (DirectoryNotFoundException ex)
        {
            _outputManager.PrintError($"Error: Solution directory not found. {ex.Message} (Path tried: '{actualSolutionPathToAnalyze}')");
        }
        catch (ArgumentException ex)
        {
            _outputManager.PrintError($"Argument Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _outputManager.PrintError($"An unexpected error occurred during analysis: {ex.Message}");
        }
    }
}