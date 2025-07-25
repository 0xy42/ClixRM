using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Models
{
    public record FormLibrary(
        string Name,
        string DisplayName
    );

    public record FormEventHandler(
        string EventName,
        string FunctionName,
        string LibraryName,
        bool Enabled,
        string? ControlId = null
    );

    public record FormAnalysisResult
    {
        public Guid FormId { get; init; } = new();
        public List<FormLibrary> Libraries { get; init; } = [];
        public List<FormEventHandler> EventHandlers { get; init; } = [];
    }
}
