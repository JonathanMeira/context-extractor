using System;
using System.Threading;
using System.Threading.Tasks;
using ContextExtrator.Domain.Models;

namespace ContextExtrator.App.Services;

public interface IContextAnalysisService
{
    IObservable<AnalysisProgress> AnalyzeProjectAsync(ProjectDescriptor descriptor, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clear the analysis cache for a specific project or all projects.
    /// </summary>
    void ClearCache(string? projectPath = null);
}

