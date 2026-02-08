using ContextExtrator.Domain.Models;
using System.Runtime.CompilerServices;

namespace ContextExtrator.Domain.Analysis;

/// <summary>
/// Service for discovering projects and files in a C# solution/project structure.
/// </summary>
public interface IDiscoveryService
{
    /// <summary>
    /// Enumerate all .csproj files in a root directory and its immediate subdirectories.
    /// </summary>
    IEnumerable<FileNode> EnumerateProjects(string rootPath);

    /// <summary>
    /// Enumerate all .cs files in a specific C# project.
    /// </summary>
    IEnumerable<FileNode> EnumerateFiles(string projectFilePath, CancellationToken ct = default);
    
    /// <summary>
    /// Enumerate files and build a hierarchical file tree for the specified project.
    /// Returns the flat file list and the built tree (one or more roots).
    /// </summary>
    Task<IEnumerable<FileTreeNode>> EnumerateFileTreeAsync(string projectFilePath, CancellationToken ct = default);
}
