using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Models.Solutions;

namespace ClixRM.Services.Solutions;

public interface ISolutionComparer
{
    Task<SolutionComparisonResult> CompareSolutionsAsync(
        string[] solutionSet1,
        string[] solutionSet2);

    Task<SolutionComparisonResult> CompareSolutionsAsync(
        string environmentName1,
        string[] solutionSet1,
        string environmentName2,
        string[] solutionSet2);
}
