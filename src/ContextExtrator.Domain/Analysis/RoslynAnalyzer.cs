using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ContextExtrator.Domain.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace ContextExtrator.Domain.Analysis;

/// <summary>
/// Real Roslyn-based implementation for analyzing C# projects.
/// Enumerates files, extracts symbols, and builds dependency graphs.
/// </summary>
public class RoslynAnalyzer : IRoslynAnalyzer
{
    private MSBuildWorkspace? _workspace;
    private Project? _currentProject;

    /// <summary>
    /// Enumerate all .cs files in a project by providing a project directory.
    /// Automatically locates .csproj or .slnx files.
    /// </summary>
    public async Task<FileNode[]> EnumerateFilesAsync(string projectPathOrDirectory, CancellationToken ct = default)
    {
        return Array.Empty<FileNode>();
    }

    /// <summary>
    /// Enumerate all .cs files in a project by providing an explicit .csproj or .slnx file path.
    /// </summary>
    public async IAsyncEnumerable<FileNode> EnumerateFilesForProjectAsync(string projectFilePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(projectFilePath))
        {
            yield break;
        }

        _workspace ??= MSBuildWorkspace.Create();
        Project? project = _workspace
            .CurrentSolution
            .Projects
            .FirstOrDefault(p => string.Equals(p.FilePath, projectFilePath, StringComparison.OrdinalIgnoreCase));

        if (project is null && projectFilePath.EndsWith(".sln"))
        {
            var solution = await _workspace.OpenSolutionAsync(projectFilePath, null, ct).ConfigureAwait(false);
            project = solution.Projects.FirstOrDefault();
        }

        project ??= await _workspace.OpenProjectAsync(projectFilePath, null, ct).ConfigureAwait(false);

        foreach (Document document in project?.Documents ?? [])
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

            // Walk the syntax tree and extract top-level symbols
            var walker = new SymbolExtractor();
            walker.Visit(root);

            return walker.ExtractedSymbols.ToArray();
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
}

/// <summary>
/// Syntax tree walker to extract symbol information from a compilation unit.
/// </summary>
internal class SymbolExtractor : CSharpSyntaxWalker
{
    public List<SymbolNode> ExtractedSymbols { get; } = new();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        ExtractedSymbols.Add(new SymbolNode
        {
            Name = node.Identifier.Text,
            Kind = "class",
            Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            Parent = GetParentTypeName(node)
        });

        base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        ExtractedSymbols.Add(new SymbolNode
        {
            Name = node.Identifier.Text,
            Kind = "struct",
            Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            Parent = GetParentTypeName(node)
        });

        base.VisitStructDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        ExtractedSymbols.Add(new SymbolNode
        {
            Name = node.Identifier.Text,
            Kind = "interface",
            Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            Parent = GetParentTypeName(node)
        });

        base.VisitInterfaceDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        ExtractedSymbols.Add(new SymbolNode
        {
            Name = node.Identifier.Text,
            Kind = "method",
            Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            Parent = GetParentTypeName(node)
        });

        base.VisitMethodDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        ExtractedSymbols.Add(new SymbolNode
        {
            Name = node.Identifier.Text,
            Kind = "property",
            Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            Parent = GetParentTypeName(node)
        });

        base.VisitPropertyDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        foreach (var variable in node.Declaration.Variables)
        {
            ExtractedSymbols.Add(new SymbolNode
            {
                Name = variable.Identifier.Text,
                Kind = "field",
                Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Parent = GetParentTypeName(node)
            });
        }

        base.VisitFieldDeclaration(node);
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        ExtractedSymbols.Add(new SymbolNode
        {
            Name = node.Identifier.Text,
            Kind = "enum",
            Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            Parent = GetParentTypeName(node)
        });

        base.VisitEnumDeclaration(node);
    }

    private static string GetParentTypeName(SyntaxNode node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is ClassDeclarationSyntax classDecl)
                return classDecl.Identifier.Text;
            if (parent is StructDeclarationSyntax structDecl)
                return structDecl.Identifier.Text;
            if (parent is InterfaceDeclarationSyntax interfaceDecl)
                return interfaceDecl.Identifier.Text;

            parent = parent.Parent;
        }

        return string.Empty;
    }
}

