using System.CommandLine;

namespace ClixRM.Sdk.Commands;

public abstract class SolutionAwareCommand : Command
{
    protected static readonly Option<string> OnlineSolutionOption = new(
        aliases: ["--online-solution", "-s"],
        description: "The unique name of the solution to download from the online environment.")
    {
        ArgumentHelpName = "solution-name"
    };

    protected static readonly Option<string> DirectoryOption = new(
        aliases: ["--dir", "-d"],
        description: "The path to the unzipped solution directory containing cloud flows.")
    {
        ArgumentHelpName = "directory-path"
    };

    protected static readonly Option<bool> ForceDownloadOption = new(
        aliases: ["--force-download", "-f"],
        description: "Force download of the solution, ignoring any cached version."
    );

    protected SolutionAwareCommand(
        string name,
        string description)
        : base(name, description)
    {
        AddOption(OnlineSolutionOption);
        AddOption(DirectoryOption);
        AddOption(ForceDownloadOption);
    }
}