using System.Threading;
using System.Threading.Tasks;
using ContextExtrator.Domain.Models;

namespace ContextExtrator.Domain.Analysis;

public interface IRoslynAnalyzer
{
    /// <summary>
    /// Enumerate files from an explicit project file (.csproj or .slnx).
    /// </summary>
    IAsyncEnumerable<FileNode> EnumerateFilesForProjectAsync(string projectFilePath, CancellationToken ct = default);

    Task<SymbolNode[]> ExtractSymbolsAsync(string filePath, CancellationToken ct = default);
    Task<GraphNode[]> ExtractDependenciesAsync(SymbolNode symbol, CancellationToken ct = default);
}
