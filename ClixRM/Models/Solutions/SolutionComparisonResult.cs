using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Models.Solutions;

public class SolutionComparisonResult
{
    public SolutionComponentSet Set1 { get; set; } = null!;
    public SolutionComponentSet Set2 { get; set; } = null!;

    public List<SolutionComponent> OnlyInSet1 { get; set; } = new();
    public List<SolutionComponent> OnlyInSet2 { get; set; } = new();
    public List<SolutionComponent> InBothSets { get; set; } = new();

    public int Set1Count => Set1.TotalComponents;
    public int Set2Count => Set2.TotalComponents;
    public int CommonCount => InBothSets.Count;
    public int Set1UniqueCount => OnlyInSet1.Count;
    public int Set2UniqueCount => OnlyInSet2.Count;
}
