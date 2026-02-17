using ContextExtrator.Domain.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Runtime.CompilerServices;

namespace ContextExtrator.Domain.Analysis;

/// <summary>
/// Real Roslyn-based implementation for analyzing C# projects.
/// Enumerates files, extracts symbols, and builds dependency graphs.
/// </summary>
public class RoslynAnalyzer : IRoslynAnalyzer
{
    private SymbolExtractor? _extractor;
    private MSBuildWorkspace? _workspace;
    private Project? _currentProject;

    /// <summary>
    /// Enumerate all .cs files in a project by providing an explicit .csproj or .sln file path.
    /// </summary>
    public async IAsyncEnumerable<FileNode> EnumerateFilesForProjectAsync(string projectFilePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(projectFilePath))
        {
            yield break;
        }

        _workspace ??= MSBuildWorkspace.Create();
        _currentProject = projectFilePath.EndsWith(".sln") 
            ? await GetSolutionFirstProjectAsync(projectFilePath, ct)
            : await GetProjectForFileAsync(projectFilePath, ct);

        foreach (Document document in _currentProject?.Documents ?? [])
        {
            ct.ThrowIfCancellationRequested();

            string documentDirectory = Path.GetDirectoryName(document.FilePath ?? string.Empty) ?? string.Empty;
            DirectoryInfo directoryInfo = new DirectoryInfo(documentDirectory);

            yield return new FileNode
            (
                Name: directoryInfo.Name,
                Path: directoryInfo.FullName,
                IsDirectory: true
            );

            yield return new FileNode
            (
                Name: Path.GetFileName(document.FilePath!),
                Path: document.FilePath ?? string.Empty,
                IsDirectory: Directory.Exists(document.FilePath)
            );
        }
    }

    /// <summary>
    /// Extract symbols (classes, methods, properties, etc.) from a specific file.
    /// </summary>
    public async Task<SymbolNode[]> ExtractSymbolsAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            if (_currentProject is null || _workspace is null)
            {
                return Array.Empty<SymbolNode>();
            }

            // Find the document by file path
            var document = _currentProject.Documents.FirstOrDefault(d => d.FilePath == filePath);
            if (document is null)
            {
                return Array.Empty<SymbolNode>();
            }

            // Get syntax tree and compilation
            var syntaxTree = await document.GetSyntaxTreeAsync(ct).ConfigureAwait(false);
            if (syntaxTree is null)
            {
                return Array.Empty<SymbolNode>();
            }

            var compilation = await document.Project.GetCompilationAsync(ct).ConfigureAwait(false);
            if (compilation is null)
            {
                return Array.Empty<SymbolNode>();
            }

            var root = await syntaxTree.GetRootAsync(ct).ConfigureAwait(false);
            var symbols = new List<SymbolNode>();
            
            _extractor = new();
            _extractor.Visit(root);
            return _extractor.ExtractedSymbols.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error extracting symbols from {filePath}: {ex.Message}");
            return Array.Empty<SymbolNode>();
        }
    }

    /// <summary>
    /// Extract dependencies (references) for a given symbol.
    /// </summary>
    public async Task<GraphNode[]> ExtractDependenciesAsync(SymbolNode symbol, CancellationToken ct = default)
    {
        try
        {
            if (_currentProject is null)
            {
                return Array.Empty<GraphNode>();
            }

            var compilation = await _currentProject.GetCompilationAsync(ct).ConfigureAwait(false);
            if (compilation is null)
            {
                return Array.Empty<GraphNode>();
            }

            // For now, return an empty dependency list
            // Real implementation would fetch the semantic model and walk references
            return Array.Empty<GraphNode>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error extracting dependencies: {ex.Message}");
            return Array.Empty<GraphNode>();
        }
    }

    private async Task<Project?> GetSolutionFirstProjectAsync(string projectFilePath, CancellationToken ct)
    {
        var solution = await _workspace!
            .OpenSolutionAsync(projectFilePath, null, ct)
            .ConfigureAwait(false);

        return solution.Projects.FirstOrDefault();
    }

    private async Task<Project?> GetProjectForFileAsync(string projectFilePath, CancellationToken ct)
    {
        Project? projectFromCache = _workspace!
            .CurrentSolution
            .Projects
            .FirstOrDefault(p => string.Equals(p.FilePath, projectFilePath, StringComparison.OrdinalIgnoreCase));

        if (projectFromCache is not null)
        {
            return projectFromCache;
        }

        return await _workspace
            .OpenProjectAsync(projectFilePath, null, ct)
            .ConfigureAwait(false);
    }
}

