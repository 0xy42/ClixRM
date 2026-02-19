using ClixRM.Sdk.Services;
using ClixRM.Services.Flows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Formatter;

public class FlowTriggersEntityMessageCommandFormatter : ICommandResultFormatter<List<FlowTriggersEntityMessageResult>>
{
    private readonly IOutputManager _outputManager;

    public FlowTriggersEntityMessageCommandFormatter(IOutputManager outputManager)
    {
        _outputManager = outputManager; 
    }

    public void Format(List<FlowTriggersEntityMessageResult> results)
    {
        if (results.Count == 0)
        {
            _outputManager.PrintWarning("No actions found performing the specified operation on the entity.");
            return;
        }

        _outputManager.PrintSuccess($"Found {results.Count} matching actions:");

        var groupedResults = results.GroupBy(r => r.FileName).OrderBy(g => g.Key);

        foreach (var group in groupedResults)
        {
            _outputManager.PrintInfo($"\n--- Flow File: {group.Key} ---");
            foreach (var result in group.OrderBy(r => r.ActionName))
            {
                _outputManager.PrintInfo(
                    $"- Action: \"{result.ActionName}\" | Type: {result.ActionType} | " +
                    $"Operation: {result.OperationId} | Entity: {result.EntityName}"
                );
            }
        }
    }
}
