using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Models.Solutions;

public class SolutionComponentSet
{
    public List<string> SolutionNames { get; set; } = new();
    public List<SolutionComponent> Components { get; set; } = new();
    public int TotalComponents => Components.Count;
}
