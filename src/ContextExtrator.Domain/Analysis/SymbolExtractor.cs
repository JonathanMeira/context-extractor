using ContextExtrator.Domain.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ContextExtrator.Domain.Analysis;

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

