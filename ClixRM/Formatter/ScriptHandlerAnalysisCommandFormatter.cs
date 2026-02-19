using ClixRM.Models;
using ClixRM.Sdk.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Formatter;

public class ScriptHandlerAnalysisCommandFormatter : ICommandResultFormatter<FormAnalysisResult>
{
    private readonly IOutputManager _outputManager;

    public ScriptHandlerAnalysisCommandFormatter(IOutputManager outputManager)
    {
        _outputManager = outputManager; 
    }

    public void Format(FormAnalysisResult result)
    {
        if (result.Libraries.Count == 0)
        {
            _outputManager.PrintWarning("No JavaScript libraries found on this form.");
            return;
        }

        _outputManager.PrintSuccess($"Found {result.Libraries.Count} JavaScript libraries.");
        foreach (var lib in result.Libraries)
        {
            _outputManager.PrintInfo($"- {lib.DisplayName} ({lib.Name})");
        }

        if (result.EventHandlers.Count == 0)
        {
            _outputManager.PrintWarning("No script event handler registered on this form.");
            return;
        }

        _outputManager.PrintSuccess($"\nFound {result.EventHandlers.Count} script event handlers.");

        var grouped = result.EventHandlers
            .OrderBy(h => h.EventName)
            .ThenBy(h => h.ControlId ?? string.Empty)
            .GroupBy(h => h.EventName);

        foreach (var evtGroup in grouped)
        {
            _outputManager.PrintInfo($"\n--- Event: {evtGroup.Key} ---");
            foreach (var handler in evtGroup)
            {
                if (string.IsNullOrEmpty(handler.ControlId))
                {
                    _outputManager.PrintInfo(
                        $"- [Form] {handler.FunctionName} (Library: {handler.LibraryName}) Enabled: {handler.Enabled}"
                    );
                }
                else
                {
                    _outputManager.PrintInfo(
                        $"- [Field: {handler.ControlId}] {handler.FunctionName} (Library: {handler.LibraryName}) Enabled: {handler.Enabled}"
                    );
                }
            }
        }
    }
}
