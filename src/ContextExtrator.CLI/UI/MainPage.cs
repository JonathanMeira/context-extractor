using ContextExtrator.Domain.Models;
using System.Reactive.Linq;
using Termina.Extensions;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;

namespace ContextExtrator.CLI.UI;

public class MainPage : ReactivePage<MainViewModel>
{
    private SelectionListNode<FileNode>? _projectsNode;
    private SelectionListNode<FileNode>? _filesNode;
    private SelectionListNode<SymbolNode>? _symbolsNode;
    private SelectionListNode<string>? _depsNode;
    protected override void OnBound()
    {
        base.OnBound();

        ViewModel.Input.OfType<KeyPressed>()
            .Where(k => k.KeyInfo.Key == ConsoleKey.Tab)
            .Subscribe(k =>
            {
                bool hasFocused = Focus.CurrentFocus is not null;
                if (!hasFocused)
                {
                    return;
                }

                List<IFocusable?> panels = [_projectsNode, _filesNode, _symbolsNode, _depsNode];

                int currentIndex = panels.IndexOf(Focus.CurrentFocus);
             
                bool isFirstNode = currentIndex == 0;
                bool isShiftPressed = k.KeyInfo.Modifiers == ConsoleModifiers.Shift;
                if (isFirstNode && isShiftPressed)
                {
                    return;
                }

                if (isShiftPressed)
                {
                    Focus.PopFocus();
                    return;
                }

                bool shouldResetFocus = currentIndex == -1;
                if (shouldResetFocus)
                {
                    Focus.PopFocus();
                    return;
                }

                bool lastIndex = currentIndex == panels.Count - 1;
                if (lastIndex)
                {
                    Focus.PushFocus(panels[0]!);
                    return;
                }

                bool hasNodeBeenCreated = panels[currentIndex + 1] is not null;

                if (hasNodeBeenCreated)
                {
                    Focus.PushFocus(panels[currentIndex + 1]!);
                }
            })
            .DisposeWith(Subscriptions);
    }

    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(
                new Termina.Layout.PanelNode()
                    .WithTitle("Dependency Extractor")
                    .WithContent(
                        ViewModel.ProjectRootChanged
                            .Select(root => new Termina.Layout.TextNode($"Root: {root}"))
                            .AsLayout())
                    .Height(3))
             .WithChild(
                new Termina.Layout.PanelNode()
                    .WithTitle("Projects (↑↓ to select)")
                    .WithContent(
                        ViewModel.ProjectFilesChanged
                            .Select(discoveredProjects =>
                            {
                                if (discoveredProjects is { Count: 0 })
                                {
                                    return (ILayoutNode) new TextNode("No projects found for current root");
                                }

                                var node = new SelectionListNode<FileNode>(discoveredProjects, project => project.Name)
                                    .WithMode(SelectionMode.Single)
                                    .WithShowNumbers()
                                    .WithVisibleRows(5);

                                node.SelectionConfirmed
                                    .Subscribe(selected =>
                                    {
                                        ViewModel.SelectedProjectFile = selected[0];
                                    })
                                    .DisposeWith(Subscriptions);

                                _projectsNode = node;
                                Focus.PushFocus(node);
                                return node;
                            })
                            .AsLayout())
                    .Height(7))
            .WithChild(
                Layouts.Horizontal()
                    .WithChild(
                        new PanelNode()
                            .WithTitle("Files (↑↓ navigate, Enter select)")
                            .WithContent(
                                ViewModel.FileTreeChanged
                                    .Select(tree =>
                                    {
                                        if (ViewModel.SelectedProjectFile == null)
                                            return (ILayoutNode)new TextNode("(select a project)");

                                        if (tree == null || tree.Count == 0)
                                            return new TextNode("(no files found)");

                                        // Flatten tree into an indented list of FileNode for display
                                        var flat = new List<FileNode>();

                                        void Recurse(FileTreeNode item, int depth)
                                        {
                                            var display = new string(' ', depth * 2) + item.Node.Name + (item.Node.IsDirectory ? "/" : "");
                                            flat.Add(new FileNode(display, item.Node.Path, item.Node.IsDirectory));
                                            foreach (var c in item.Children)
                                                Recurse(c, depth + 1);
                                        }

                                        foreach (var root in tree)
                                            Recurse(root, 0);

                                        var node = new SelectionListNode<FileNode>(
                                                flat,
                                                f => f.Name)
                                            .WithMode(SelectionMode.Single)
                                            .WithShowNumbers(false)
                                            .WithVisibleRows(10);

                                        node.SelectionConfirmed
                                            .Subscribe(selected =>
                                            {
                                                bool isDirectory = selected[0].IsDirectory;
                                                if (!isDirectory)
                                                {
                                                    ViewModel.SelectedFile = selected[0];
                                                }
                                            })
                                            .DisposeWith(Subscriptions);

                                        _filesNode = node;
                                        Focus.PushFocus(node);

                                        return node;
                                    })
                                    .AsLayout())
                            .Width(35))
                    .WithChild(
                        Layouts.Vertical()
                            .WithChild(
                                new Termina.Layout.PanelNode()
                                    .WithTitle("Symbols (↑↓ navigate, Enter select)")
                                    .WithContent(
                                        ViewModel.SymbolsChanged
                                            .Select(symbols =>
                                            {
                                                if (ViewModel.SelectedFile == null)
                                                    return (ILayoutNode)new Termina.Layout.TextNode("(select a file)");

                                                if (symbols.Count == 0)
                                                    return new Termina.Layout.TextNode("(no symbols found)");

                                                var node = new Termina.Layout.SelectionListNode<SymbolNode>(
                                                        symbols,
                                                        s => s.Name)
                                                    .WithMode(Termina.Layout.SelectionMode.Single)
                                                    .WithShowNumbers(true)
                                                    .WithVisibleRows(6);

                                                node.SelectionConfirmed
                                                    .Subscribe(selected =>
                                                    {
                                                        if (selected.FirstOrDefault() is SymbolNode symbol)
                                                            ViewModel.SelectedSymbol = symbol;
                                                    })
                                                    .DisposeWith(Subscriptions);

                                                _symbolsNode = node;
                                                Focus.PushFocus(node);

                                                return node;
                                            })
                                            .AsLayout())
                                    .Height(6))
                            .WithChild(
                                new Termina.Layout.PanelNode()
                                    .WithTitle("Dependencies")
                                    .WithContent(
                                        ViewModel.DependenciesChanged
                                            .Select(list =>
                                            {
                                                if (list.Count == 0)
                                                    return (ILayoutNode)new Termina.Layout.TextNode("(select a symbol)");

                                                var names = list.Select(d => d.Name).ToList();
                                                var node = new Termina.Layout.SelectionListNode<string>(
                                                    names,
                                                    s => s)
                                                    .WithMode(Termina.Layout.SelectionMode.Single)
                                                    .WithShowNumbers(false)
                                                    .WithVisibleRows(6);

                                                _depsNode = node;
                                                Focus.PushFocus(node);

                                                return (ILayoutNode)node;
                                            })
                                            .AsLayout())
                                    .Fill())
                            .Fill()))
            .WithChild(
                Layouts.Horizontal()
                    .WithChild(new Termina.Layout.TextNode("Projects/Files: ↑↓ navigate, Enter select | Esc quit").NoWrap())
                    .Height(1));
    }
}
