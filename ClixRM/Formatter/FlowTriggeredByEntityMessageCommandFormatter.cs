using ClixRM.Sdk.Services;
using ClixRM.Services.Flows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Formatter;

public class FlowTriggeredByEntityMessageCommandFormatter : ICommandResultFormatter<List<TriggeredByEntityMessageResult>>
{
    private readonly IOutputManager _outputManager;

    public FlowTriggeredByEntityMessageCommandFormatter(IOutputManager outputManager)
    {
        _outputManager = outputManager; 
    }

    public void Format(List<TriggeredByEntityMessageResult> results)
    {
        if (results.Count == 0)
        {
            _outputManager.PrintWarning("No triggers found for the specified entity and event.");
            return;
        }

        _outputManager.PrintSuccess($"Found {results.Count} matching triggers:");

        var groupedResults = results.GroupBy(r => r.FileName).OrderBy(g => g.Key);

        foreach (var group in groupedResults)
        {
            _outputManager.PrintInfo($"\n--- Flow File: {group.Key} ---");
            foreach (var result in group.OrderBy(r => r.TriggerName))
            {
                _outputManager.PrintInfo(
                    $"- Trigger: \"{result.TriggerName}\" | Event: {result.EventName} | " +
                    $"Scope: {result.Scope} | Entity: {result.EntityName}"
                );
            }
        }
    }
}
