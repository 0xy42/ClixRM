using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Commands.Solution
{
    public class SolutionCommand : Command
    {
        public SolutionCommand(
            SolutionComparerCommand solutionComparerCommand
            ) : base ("solution", "Commands for analyzing and interacting with solutions.")
        {
            AddCommand(solutionComparerCommand);
        }
    }
}
