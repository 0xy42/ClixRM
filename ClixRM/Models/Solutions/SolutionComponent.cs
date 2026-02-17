using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Models.Solutions;

public record SolutionComponent(
    Guid ComponentId,
    int ComponentType,
    string ComponentTypeName,
    string? LogicalName,
    string? DisplayName);
