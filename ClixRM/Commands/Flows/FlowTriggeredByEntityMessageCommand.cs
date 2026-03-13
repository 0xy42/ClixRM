using ClixRM.Services.Flows;
using ClixRM.Services.Output;
using ClixRM.Services.Solutions;
using System.CommandLine;
using System.CommandLine.Parsing;
using ClixRM.Sdk.Commands;
using ClixRM.Sdk.Services;

namespace ClixRM.Commands.Flows;

public class FlowTriggeredByEntityMessageCommand : SolutionAwareCommand<List<TriggeredByEntityMessageResult>>
{
    private readonly FlowTriggeredByEntityMessageAnalyzer _analyzer;
    private readonly IOutputManager _outputManager;
    private readonly ISolutionPathResolver _solutionPathResolver;

    public FlowTriggeredByEntityMessageCommand(
        IOutputManager outputManager,
        ISolutionPathResolver solutionPathResolver,
        IActiveConnectionGuard activeConnectionGuard,
        ICommandResultFormatter<List<TriggeredByEntityMessageResult>> formatter)
        : base("triggered-by-message",
               "Check all flows in a solution for triggers on a specific entity and message.",
               activeConnectionGuard,
               formatter)
    {
        _outputManager = outputManager;
        _solutionPathResolver = solutionPathResolver;

        var entityOption = CreateEntityOption();
        var messageOption = CreateMessageOption();

        AddOption(entityOption);
        AddOption(messageOption);

        _analyzer = new FlowTriggeredByEntityMessageAnalyzer();

        this.SetHandler(
            HandleCommandAsync,
            entityOption,
            messageOption,
            OnlineSolutionOption,
            DirectoryOption,
            ForceDownloadOption
        );
    }

    private static Option<string> CreateEntityOption()
    {
        var entityOption = new Option<string>(["--entity", "-e"], "The logical singular name of the entity to check.")
        {
            IsRequired = true,
            ArgumentHelpName = "entity"
        };
        return entityOption;
    }

    private static Option<string> CreateMessageOption()
    {
        var messageOption = new Option<string>(["--message", "-m"], "The name of the event message (create, update, delete)")
        {
            IsRequired = true,
            ArgumentHelpName = "event"
        };
        messageOption.AddValidator(ValidateMessage);
        return messageOption;
    }

    private static void ValidateMessage(OptionResult optionResult)
    {
        var value = optionResult.GetValueOrDefault<string>();
        string[] allowedEvents = ["create", "update", "delete"];
        if (string.IsNullOrWhiteSpace(value) || !allowedEvents.Contains(value.ToLowerInvariant()))
        {
            optionResult.ErrorMessage = $"Event type must be one of '{string.Join("', '", allowedEvents)}'. Received: '{value}'";
        }
    }

    private async Task HandleCommandAsync(
        string entityName, string messageName,
        string? onlineSolutionName, string? directoryPath, bool forceDownload)
    {
        _outputManager.PrintInfo("Resolving solution path...");
        var actualSolutionPathToAnalyze = await _solutionPathResolver.ResolveSolutionPathAsync(onlineSolutionName, directoryPath, forceDownload);

        if (string.IsNullOrEmpty(actualSolutionPathToAnalyze))
        {
            return;
        }

        _outputManager.PrintInfo($"Searching for flows triggered by event '{messageName}' on entity '{entityName}' in directory '{actualSolutionPathToAnalyze}'...");
        try
        {
            var results = _analyzer.AnalyzeTriggerUsage(actualSolutionPathToAnalyze, entityName, messageName);
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