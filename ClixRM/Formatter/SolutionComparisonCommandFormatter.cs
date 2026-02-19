using ClixRM.Models.Solutions;
using ClixRM.Sdk.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Formatter;

public class SolutionComparisonCommandFormatter : ICommandResultFormatter<SolutionComparisonResult>
{
    private readonly IOutputManager _outputManager;

    public SolutionComparisonCommandFormatter(IOutputManager outputManager)
    {
        _outputManager = outputManager;
    }

    public void Format(SolutionComparisonResult result)
    {
        _outputManager.PrintSuccess("=== Comparison Summary ===");
        _outputManager.PrintInfo("");
        _outputManager.PrintInfo($"Total Components in Set 1: {result.Set1Count}");
        _outputManager.PrintInfo($"Total Components in Set 2: {result.Set2Count}");
        _outputManager.PrintInfo($"Common Components:         {result.CommonCount}");
        _outputManager.PrintInfo($"Only in Set 1:             {result.Set1UniqueCount}");
        _outputManager.PrintInfo($"Only in Set 2:             {result.Set2UniqueCount}");
        _outputManager.PrintInfo("");

        if (result.OnlyInSet1.Count != 0)
        {
            _outputManager.PrintWarning($"=== Components Only in Set 1 ({result.Set1UniqueCount}) ===");
            PrintComponentList(result.OnlyInSet1);
            _outputManager.PrintInfo("");
        }

        if (result.OnlyInSet2.Count != 0)
        {
            _outputManager.PrintWarning($"=== Components Only in Set 2 ({result.Set2UniqueCount}) ===");
            PrintComponentList(result.OnlyInSet2);
            _outputManager.PrintInfo("");
        }

        if (result.Set1UniqueCount == 0 && result.Set2UniqueCount == 0)
        {
            _outputManager.PrintSuccess("The two solution sets are identical in terms of components.");
        }
        else
        {
            _outputManager.PrintWarning("The two solution sets have differences in their components.");
        }
    }

    private void PrintComponentList(List<SolutionComponent> components)
    {
        var groupedComponents = components
            .GroupBy(c => c.ComponentType)
            .OrderBy(g => g.Key);

        foreach (var group in groupedComponents)
        {
            var componentTypeName = group.First().ComponentTypeName;
            _outputManager.PrintInfo($"  {componentTypeName} {group.Key}: {group.Count()} component(s)");

            foreach (var component in group.Take(10))
            {
                _outputManager.PrintInfo($"    - {component.ComponentId}");
            }

            if (group.Count() > 10)
            {
                _outputManager.PrintInfo($"    ... and {group.Count() - 10} more");
            }
        }
    }
}
