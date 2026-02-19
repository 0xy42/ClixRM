using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Models;
using ClixRM.Sdk.Commands;
using ClixRM.Sdk.Services;
using ClixRM.Services.Forms;
using ClixRM.Services.Output;

namespace ClixRM.Commands.Forms
{
    public class ScriptHandlerAnalysisCommand : CrmConnectedCommand<FormAnalysisResult>
    {
        private readonly IOutputManager _outputManager;
        private readonly IFormAnalyzer _formAnalyzer;

        public ScriptHandlerAnalysisCommand(
            IOutputManager outputManager, 
            IFormAnalyzer formAnalyzer, 
            IActiveConnectionGuard activeConnectionGuard,
            ICommandResultFormatter<FormAnalysisResult> formatter) 
            : base("script-handler-analysis", "Analyze form scripts for registered JavaScript handlers.", activeConnectionGuard, formatter)
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

                Formatter.Format(analysis);
            }
            catch (Exception ex)
            {
                _outputManager.PrintError($"An error occurred during form analysis: {ex.Message}");
            }
        }
    }
}
