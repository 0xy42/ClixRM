using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Commands.Forms
{
    public class FormCommand : Command
    {
        public FormCommand(ScriptHandlerAnalysisCommand scriptHandlerAnalysisCommand) : base("form", "Commands for analysis and utilities regarding forms.")
        {
            AddCommand(scriptHandlerAnalysisCommand);
        }
    }
}
