using ClixRM.Sdk.Services;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace ClixRM.Sdk.Commands;

public abstract class SolutionAwareCommand<TResult> : ClixRMBaseCommand<TResult>
{
    private readonly IActiveConnectionGuard _activeConnectionGuard;

    protected SolutionAwareCommand(
        string name,
        string description,
        IActiveConnectionGuard activeConnectionGuard,
        ICommandResultFormatter<TResult> formatter)
        : base(name, description, formatter)
    {
        _activeConnectionGuard = activeConnectionGuard;

        AddOption(OnlineSolutionOption);
        AddOption(DirectoryOption);
        AddOption(ForceDownloadOption);

        AddValidator(ValidateSolutionMode);
    }

    protected static readonly Option<string> OnlineSolutionOption = new(
    aliases: ["--online-solution", "-s"],
    description: "The unique name of the solution to download from the online environment.")
    {
        ArgumentHelpName = "solution-name"
    };

    protected static readonly Option<string> DirectoryOption = new(
        aliases: ["--dir", "-d"],
        description: "The path to the unzipped solution directory.")
    {
        ArgumentHelpName = "directory-path"
    };

    protected static readonly Option<bool> ForceDownloadOption = new(
        aliases: ["--force-download", "-f"],
        description: "Force download of the solution, ignoring any cached version."
    );

    private void ValidateSolutionMode(CommandResult result)
    {
        var onlineSolution = result.FindResultFor(OnlineSolutionOption)?.GetValueOrDefault<string>();
        var directory = result.FindResultFor(DirectoryOption)?.GetValueOrDefault<string>();

        if (!string.IsNullOrEmpty(onlineSolution) && !string.IsNullOrEmpty(directory))
        {
            result.ErrorMessage = "Cannot specify both --online-solution and --dir. Use only one of those flags at a time.";
            return;
        }

        if (string.IsNullOrEmpty(onlineSolution) && string.IsNullOrEmpty(directory))
        {
            result.ErrorMessage = "Must specify either --online-solution or --dir.";
            return;
        }

        if (!string.IsNullOrEmpty(onlineSolution) && !_activeConnectionGuard.DoesActiveConnectionExist())
        {
            result.ErrorMessage = "Online solution mode (--online-solution) requires an active Dataverse connection. Please use the 'auth' command to log in first.";
        }
    }
}