using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Sdk.Commands;
using ClixRM.Sdk.Services;
using ClixRM.Services.Forms;
using ClixRM.Services.Output;

namespace ClixRM.Commands.Forms
{
    public class ScriptHandlerAnalysisCommand : CrmConnectedCommand
    {
        private readonly IOutputManager _outputManager;
        private readonly IFormAnalyzer _formAnalyzer;

        public ScriptHandlerAnalysisCommand(IOutputManager outputManager, IFormAnalyzer formAnalyzer, IActiveConnectionGuard activeConnectionGuard) 
            : base("script-handler-analysis", "Analyze form scripts for registered JavaScript handlers.", activeConnectionGuard)
        {
            _outputManager = outputManager;
            _formAnalyzer = formAnalyzer;

            var entityNameOption = CreateEntityNameOption();
            var formNameOption = CreateFormGuidOption();

            AddOption(entityNameOption);
            AddOption(formNameOption);

            this.SetHandler(HandleScriptHandlerAnalysisAsync, entityNameOption, formNameOption);
        }

        private static Option<string> CreateEntityNameOption()
        {
            return new Option<string>(["--entity", "-e"], "The logical name of the entity.")
            {
                IsRequired = true,
                ArgumentHelpName = "entity"
            };
        }

        private static Option<Guid> CreateFormGuidOption()
        {
            return new Option<Guid>(["--formId", "-f"], "The GUID of the form to analyze.")
            {
                IsRequired = true,
                ArgumentHelpName = "name"
            };
        }

        private async Task HandleScriptHandlerAnalysisAsync(string entityName, Guid formId)
        {
            try
            {
                var analysis = await _formAnalyzer.AnalyzeFormAsync(entityName, formId);

                if (analysis.Libraries.Count == 0)
                {
                    _outputManager.PrintWarning("No JavaScript libraries found on this form.");
                    return;
                }

                _outputManager.PrintSuccess($"Found {analysis.Libraries.Count} JavaScript libraries.");
                foreach(var lib in analysis.Libraries)
                {
                    _outputManager.PrintInfo($"- {lib.DisplayName} ({lib.Name})");
                }

                if (analysis.EventHandlers.Count == 0)
                {
                    _outputManager.PrintWarning("No script event handler registered on this form.");
                } 
                else
                {
                    _outputManager.PrintSuccess($"\nFound {analysis.EventHandlers.Count} script event handlers.");

                    var grouped = analysis.EventHandlers
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
                                    $"- [Form] {handler.FunctionName} (Library: {handler.LibraryName}) " +
                                    $"Enabled: {handler.Enabled}"
                                );
                            }
                            else
                            {
                                _outputManager.PrintInfo(
                                    $"- [Field: {handler.ControlId}] {handler.FunctionName} (Library: {handler.LibraryName}) " +
                                    $"Enabled: {handler.Enabled}"
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _outputManager.PrintError($"An error occurred during form analysis: {ex.Message}");
            }
        }
    }
}
