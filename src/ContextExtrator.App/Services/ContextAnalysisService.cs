using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContextExtrator.Domain.Analysis;
using ContextExtrator.Domain.Models;

namespace ContextExtrator.App.Services;

public class ContextAnalysisService : IContextAnalysisService
{
    private readonly IRoslynAnalyzer _analyzer;
    private readonly Dictionary<string, AnalysisResult> _analysisCache = new();
    private readonly Dictionary<string, FileNode[]> _fileEnumerationCache = new();

    public ContextAnalysisService(IRoslynAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public IObservable<AnalysisProgress> AnalyzeProjectAsync(ProjectDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        return Observable.Create<AnalysisProgress>(async (obs, ct) =>
        {
            try
            {
                // Check cache first
                if (_analysisCache.TryGetValue(descriptor.Path, out var cachedResult))
                {
                    obs.OnNext(new AnalysisProgress 
                    { 
                        Phase = AnalysisPhase.Complete, 
                        CurrentFile = "Using cached results",
                        FilesProcessed = cachedResult.Files.Length,
                        TotalFiles = cachedResult.Files.Length
                    });
                    obs.OnCompleted();
                    return;
                }

                var files = new List<FileNode>();
                var symbols = new List<SymbolNode>();
                var dependencies = new List<GraphNode>();

                // Phase 1: File Enumeration (Scanning)
                obs.OnNext(new AnalysisProgress 
                { 
                    Phase = AnalysisPhase.Scanning, 
                    CurrentFile = "Enumerating files...",
                    FilesProcessed = 0,
                    TotalFiles = 0
                });

                var enumeratedFiles = await _analyzer.EnumerateFilesAsync(descriptor.Path, ct).ConfigureAwait(false);
                files.AddRange(enumeratedFiles ?? Array.Empty<FileNode>());
                int totalFiles = files.Count;

                if (totalFiles == 0)
                {
                    obs.OnNext(new AnalysisProgress 
                    { 
                        Phase = AnalysisPhase.Complete, 
                        CurrentFile = "No files found",
                        FilesProcessed = 0,
                        TotalFiles = 0
                    });
                    obs.OnCompleted();
                    return;
                }

                // Phase 2: Symbol Extraction (Analyzing)
                obs.OnNext(new AnalysisProgress 
                { 
                    Phase = AnalysisPhase.Analyzing, 
                    CurrentFile = "Starting symbol extraction...",
                    FilesProcessed = 0,
                    TotalFiles = totalFiles
                });

                int filesProcessed = 0;
                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    filesProcessed++;
                    obs.OnNext(new AnalysisProgress
                    {
                        FilesProcessed = filesProcessed,
                        TotalFiles = totalFiles,
                        CurrentFile = file.Path,
                        Phase = AnalysisPhase.Analyzing
                    });

                    var fileSymbols = await _analyzer.ExtractSymbolsAsync(file.Path, ct).ConfigureAwait(false);
                    if (fileSymbols != null && fileSymbols.Length > 0)
                    {
                        symbols.AddRange(fileSymbols);
                    }
                }

                // Phase 3: Dependency Graph Building (BuildingGraph)
                obs.OnNext(new AnalysisProgress 
                { 
                    Phase = AnalysisPhase.BuildingGraph, 
                    CurrentFile = "Building dependency graph...",
                    FilesProcessed = filesProcessed,
                    TotalFiles = totalFiles
                });

                // Extract dependencies for each symbol
                int symbolsProcessed = 0;
                foreach (var symbol in symbols)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    var symDeps = await _analyzer.ExtractDependenciesAsync(symbol, ct).ConfigureAwait(false);
                    if (symDeps != null && symDeps.Length > 0)
                    {
                        dependencies.AddRange(symDeps);
                    }
                    
                    symbolsProcessed++;
                }

                // Cache the result
                var result = new AnalysisResult
                {
                    Files = files.ToArray(),
                    Symbols = symbols.ToArray(),
                    Dependencies = dependencies.ToArray()
                };
                _analysisCache[descriptor.Path] = result;

                // Phase 4: Complete
                obs.OnNext(new AnalysisProgress 
                { 
                    Phase = AnalysisPhase.Complete, 
                    CurrentFile = $"Analysis complete: {totalFiles} files, {symbols.Count} symbols",
                    FilesProcessed = filesProcessed,
                    TotalFiles = totalFiles
                });
                
                obs.OnCompleted();
            }
            catch (OperationCanceledException)
            {
                obs.OnNext(new AnalysisProgress 
                { 
                    Phase = AnalysisPhase.Error, 
                    CurrentFile = "Analysis cancelled by user"
                });
                obs.OnError(new OperationCanceledException());
            }
            catch (Exception ex)
            {
                obs.OnNext(new AnalysisProgress 
                { 
                    Phase = AnalysisPhase.Error, 
                    CurrentFile = $"Error: {ex.Message}"
                });
                obs.OnError(ex);
            }
        });
    }

    /// <summary>
    /// Clear the analysis cache for a specific project or all projects.
    /// </summary>
    public void ClearCache(string? projectPath = null)
    {
        if (projectPath != null)
        {
            _analysisCache.Remove(projectPath);
            _fileEnumerationCache.Remove(projectPath);
        }
        else
        {
            _analysisCache.Clear();
            _fileEnumerationCache.Clear();
        }
    }
}