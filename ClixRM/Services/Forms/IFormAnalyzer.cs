using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClixRM.Models;

namespace ClixRM.Services.Forms
{
    public interface IFormAnalyzer
    {
        Task<FormAnalysisResult> AnalyzeFormAsync(string entityName, Guid formName);
    }
}
