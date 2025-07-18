using System.CommandLine;

namespace ClixRM.Commands.Flows
{
    public class FlowCommand : Command
    {
        public FlowCommand
        (
            ColumnDependencyCheckCommand columnDependencyCheckCommand,
            FlowTriggeredByEntityMessageCommand flowTriggeredByEntityMessage,
            FlowTriggersEntityMessageCommand flowTriggersEntityMessageCommand
        )
            : base("flow", "Commands for analysis and utilities regarding cloud flows.")
        {
            AddCommand(columnDependencyCheckCommand);
            AddCommand(flowTriggeredByEntityMessage);
            AddCommand(flowTriggersEntityMessageCommand);
        }
    }
}
