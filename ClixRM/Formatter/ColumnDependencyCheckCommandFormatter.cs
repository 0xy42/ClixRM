using ClixRM.Sdk.Services;
using ClixRM.Services.Flows;
using ClixRM.Services.Output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Formatter;

public class ColumnDependencyCheckCommandFormatter : ICommandResultFormatter<List<FieldDependencyResult>>
{
    private readonly IOutputManager _outputManager;

    public ColumnDependencyCheckCommandFormatter(IOutputManager outputManager)
    {
        _outputManager = outputManager; 
    }

    public void Format(List<FieldDependencyResult> results)
    {
        if (results.Count == 0)
        {
            _outputManager.PrintWarning("No dependencies found for the specified field.");
            return;
        }

        _outputManager.PrintSuccess($"Found {results.Count} dependencies:");

        var groupedResults = results.GroupBy(r => r.FileName).OrderBy(g => g.Key);

        foreach (var group in groupedResults)
        {
            _outputManager.PrintInfo($"\n--- Flow File: {group.Key} ---");
            foreach (var result in group.OrderBy(r => r.SourceType).ThenBy(r => r.SourceName))
            {
                _outputManager.PrintInfo(
                    $"- {result.SourceType}: \"{result.SourceName}\" | Type: {result.DependencyType} | " +
                    $"Entity: {result.EntityName} | Field: {result.FieldName} | Details: {result.Details}"
                );
            }
        }
    }
}
