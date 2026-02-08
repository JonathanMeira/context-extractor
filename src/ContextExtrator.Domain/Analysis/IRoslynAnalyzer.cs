using System.Threading;
using System.Threading.Tasks;
using ContextExtrator.Domain.Models;

namespace ContextExtrator.Domain.Analysis;

public interface IRoslynAnalyzer
{
    /// <summary>
    /// Enumerate files from a project directory or explicit project file.
    /// Automatically resolves .csproj/.slnx files if a directory is provided.
    /// </summary>
    Task<FileNode[]> EnumerateFilesAsync(string projectPathOrDirectory, CancellationToken ct = default);

    /// <summary>
    /// Enumerate files from an explicit project file (.csproj or .slnx).
    /// </summary>
    IAsyncEnumerable<FileNode> EnumerateFilesForProjectAsync(string projectFilePath, CancellationToken ct = default);

    Task<SymbolNode[]> ExtractSymbolsAsync(string filePath, CancellationToken ct = default);
    Task<GraphNode[]> ExtractDependenciesAsync(SymbolNode symbol, CancellationToken ct = default);
}
