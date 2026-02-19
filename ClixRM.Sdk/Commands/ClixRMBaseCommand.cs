using ClixRM.Sdk.Services;
using System.CommandLine;

namespace ClixRM.Sdk.Commands;

public abstract class ClixRMBaseCommand<TResult> : Command
{
    protected readonly ICommandResultFormatter<TResult> Formatter;

    protected ClixRMBaseCommand(string name, string description, ICommandResultFormatter<TResult> formatter) : base(name, description)
    {
        Formatter = formatter;
    }
}
