using ClixRM.Services.Flows;
using ClixRM.Services.Output;
using ClixRM.Services.Solutions;
using System.CommandLine;
using System.CommandLine.Parsing;
using ClixRM.Sdk.Commands;
using ClixRM.Sdk.Services;

namespace ClixRM.Commands.Flows;
/// <summary>
///     Command to find cloud flows performing specific actions (Create, Update, Delete, etc.) on a given entity.
/// </summary>
public class FlowTriggersEntityMessageCommand : SolutionAwareCommand<List<FlowTriggersEntityMessageResult>>
{
    private readonly FlowTriggersEntityMessageAnalyzer _analyzer;
    private readonly IOutputManager _outputManager;
    private readonly ISolutionPathResolver _solutionPathResolver;

    private static readonly string[] AllowedOperations = FlowAttributes.ActionNameToOperationIdMap.Keys.ToArray();

    public FlowTriggersEntityMessageCommand(
        IOutputManager outputManager,
        ISolutionPathResolver solutionPathResolver,
        IActiveConnectionGuard activeConnectionGuard,
        ICommandResultFormatter<List<FlowTriggersEntityMessageResult>> formatter)
        : base("triggers-message",
               "Check all flows in a solution for triggering specific entity messages (e.g. create account).",
               activeConnectionGuard,
               formatter)
    {
        _outputManager = outputManager;
        _solutionPathResolver = solutionPathResolver;

        var entityOption = CreateEntityOption();
        var operationOption = CreateOperationOption();

        AddOption(entityOption);
        AddOption(operationOption);

        _analyzer = new FlowTriggersEntityMessageAnalyzer();

        this.SetHandler(
            HandleCommandAsync,
            entityOption,
            operationOption,
            OnlineSolutionOption,
            DirectoryOption,
            ForceDownloadOption
        );
    }

    private static Option<string> CreateEntityOption()
    {
        var entityOption = new Option<string>(
            aliases: ["--entity", "-e"],
            description: "The logical singular name of the entity targeted by the action.")
        {
            IsRequired = true,
            ArgumentHelpName = "entityName"
        };
        return entityOption;
    }

    private static Option<string> CreateOperationOption()
    {
        var operationOption = new Option<string>(
            aliases: ["--message", "-m"],
            description: $"The type of operation to search for actions performing. Allowed values: {string.Join(", ", AllowedOperations)}.")
        {
            IsRequired = true,
            ArgumentHelpName = "operationName"
        };
        operationOption.AddValidator(ValidateOperationName);
        return operationOption;
    }

    private static void ValidateOperationName(OptionResult optionResult)
    {
        var value = optionResult.GetValueOrDefault<string>();
        if (!string.IsNullOrWhiteSpace(value) &&
            !AllowedOperations.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            optionResult.ErrorMessage = $"Operation name must be one of '{string.Join("', '", AllowedOperations)}'. Received: '{value}'";
        }
    }
    private async Task HandleCommandAsync(
        string entityName, string messageName,
        string? onlineSolutionName, string? directoryPath, bool forceDownload)
    {
        _outputManager.PrintInfo("Resolving solution path...");
        string? actualSolutionPathToAnalyze = await _solutionPathResolver.ResolveSolutionPathAsync(onlineSolutionName, directoryPath, forceDownload);

        if (string.IsNullOrEmpty(actualSolutionPathToAnalyze))
        {
            return;
        }

        _outputManager.PrintInfo($"Searching for flows with actions performing '{messageName}' on entity '{entityName}' in directory '{actualSolutionPathToAnalyze}'...");
        try
        {
            var results = _analyzer.AnalyzeActionUsage(actualSolutionPathToAnalyze, entityName, messageName);

            Formatter.Format(results);
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
            _outputManager.PrintError($"An unexpected error occurred: {ex.Message}");
        }
    }
}