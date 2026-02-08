using System.Runtime.CompilerServices;
using ContextExtrator.Domain.Models;

namespace ContextExtrator.Domain.Analysis;

/// <summary>
/// Implementation of discovery service for C# projects and files.
/// Uses Roslyn analyzer for file enumeration from projects.
/// </summary>
public class DiscoveryService : IDiscoveryService
{
    private readonly IRoslynAnalyzer _roslynAnalyzer;

    public DiscoveryService(IRoslynAnalyzer roslynAnalyzer)
    {
        _roslynAnalyzer = roslynAnalyzer;
    }

    /// <summary>
    /// Enumerate all .csproj files in a root directory and its immediate subdirectories.
    /// </summary>
    public IEnumerable<FileNode> EnumerateProjects(string rootPath)
    {
        return Directory
            .EnumerateFiles(rootPath, "*.csproj", SearchOption.AllDirectories)
            .Select(filePath => new FileNode(
                Name: Path.GetFileName(filePath),
                Path: filePath,
                IsDirectory: false));
    }

    /// <summary>
    /// Enumerate all .cs files in a specific C# project using Roslyn analyzer.
    /// </summary>
    public IEnumerable<FileNode> EnumerateFiles(string projectFilePath, CancellationToken ct = default)
    {
        string? directoryPath = Path.GetDirectoryName(projectFilePath);

        return Directory
            .EnumerateFiles(directoryPath!, ".cs", SearchOption.AllDirectories)
            .Select(filePath => new FileNode(
                Name: Path.GetFileName(filePath),
                Path: filePath,
                IsDirectory: false));
    }


    public async Task<IEnumerable<FileTreeNode>> EnumerateFileTreeAsync(string projectFilePath, CancellationToken ct = default)
    {
        List<FileNode> enumeratedFiles = await _roslynAnalyzer
            .EnumerateFilesForProjectAsync(projectFilePath, ct)
            .ToListAsync(cancellationToken: ct);

        return enumeratedFiles
            .Where(fn => fn.IsDirectory)
            .DistinctBy(fn => fn.Path)
            .GroupJoin(enumeratedFiles,
                tp => tp.Path,
                fn => Path.GetDirectoryName(fn.Path),
                (tp, fn) =>
                {
                    FileTreeNode rootNode = new(tp);
                    rootNode.Children.AddRange(fn.Select(fn => new FileTreeNode(fn)));

                    return rootNode;
                });
    }

}
