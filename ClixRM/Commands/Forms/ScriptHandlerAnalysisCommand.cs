using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Services.Forms;
using ClixRM.Services.Output;

namespace ClixRM.Commands.Forms
{
    public class ScriptHandlerAnalysisCommand : CrmConnectedCommand
    {
        private readonly IOutputManager _outputManager;
        private readonly IFormAnalyzer _formAnalyzer;

        public ScriptHandlerAnalysisCommand(IOutputManager outputManager, IFormAnalyzer formAnalyzer) 
            : base("script-handler-analysis", "Analyze form scripts for registered JavaScript handlers.")
        {
            _outputManager = outputManager;
            _formAnalyzer = formAnalyzer;

            var entityNameOption = CreateEntityNameOption();
            var formNameOption = CreateFormNameOption();

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

        private static Option<string> CreateFormNameOption()
        {
            return new Option<string>(["--formName", "-f"], "The logical name of the form to analyze.")
            {
                IsRequired = true,
                ArgumentHelpName = "name"
            };
        }

        private async Task HandleScriptHandlerAnalysisAsync(string entityName, string formName)
        {
            try
            {
                var analysis = await _formAnalyzer.AnalyzeFormAsync(entityName, formName);

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
                                    $"- [Field: {handler.ControlId}] {handler.FunctionName} (Library: {handler.LibraryName} " +
                                    $"Enabled: {handler.Enabled}"
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _outputManager.PrintError($"An error occurred during privilege check: {ex.Message}");
            }
        }
    }
}
