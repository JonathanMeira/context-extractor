using ContextExtrator.Domain.Analysis;
using ContextExtrator.Domain.Models;
using System.Reactive.Linq;
using Termina.Reactive;

namespace ContextExtrator.CLI.UI;

public partial class MainViewModel : ReactiveViewModel
{
    private readonly IRoslynAnalyzer _roslynAnalyzer;
    private readonly IDiscoveryService _discoveryService;

    public MainViewModel(IRoslynAnalyzer roslynAnalyzer, IDiscoveryService discoveryService)
    {
        _roslynAnalyzer = roslynAnalyzer;
        _discoveryService = discoveryService;
    }

    [Reactive] private string _projectRoot = "E:\\workspaces\\ws-dotnet-2026\\context-extractor";

    [Reactive] private List<FileNode> _projectFiles = [];
    [Reactive] private FileNode? _selectedProjectFile;


    [Reactive] private List<FileTreeNode> _fileTree = new();
    [Reactive] private FileNode? _selectedFile;

    [Reactive] private List<SymbolNode> _symbols = [];
    [Reactive] private SymbolNode? _selectedSymbol;

    [Reactive] private List<GraphNode> _dependencies = new();


    public override void OnActivated()
    {
        ProjectRootChanged
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Subscribe(rootChange =>
            {
                IEnumerable<FileNode> projects = _discoveryService.EnumerateProjects(rootChange);
                ProjectFiles = [.. projects];
            })
            .DisposeWith(Subscriptions);

        SelectedProjectFileChanged
            .Where(f => f is not null)
            .Do(_ =>
            {
                SelectedFile = null;
                Symbols = [];
                SelectedSymbol = null;
                Dependencies = [];
            })
            .SelectMany(selectedProject => Observable.FromAsync(() => EnumerateProjectFilesAsync(selectedProject!)))
            .Subscribe(tree => FileTree = [.. tree])
            .DisposeWith(Subscriptions);

        SelectedFileChanged
            .Where(f => f is not null)
            .Do(_ =>
            {
                SelectedSymbol = null;
                Dependencies = [];
            })
            .SelectMany(file => Observable.FromAsync(() => ExtractSymbolsForFileAsync(file!)))
            .Subscribe(symbols => Symbols = [.. symbols])
            .DisposeWith(Subscriptions);

        SelectedSymbolChanged
            .Where(s => s is not null)
            .SelectMany(symbol => Observable.FromAsync(() => ExtractDependenciesForSymbolAsync(symbol!)))
            .Subscribe(deps => Dependencies = [.. deps])
            .DisposeWith(Subscriptions);
    }

    private async Task<IEnumerable<FileTreeNode>> EnumerateProjectFilesAsync(FileNode project)
    {
         return await _discoveryService
            .EnumerateFileTreeAsync(project.Path, CancellationToken.None)
            .ConfigureAwait(false);
    }

    private async Task<IEnumerable<SymbolNode>> ExtractSymbolsForFileAsync(FileNode file)
    {
        return await _roslynAnalyzer
            .ExtractSymbolsAsync(file.Path, CancellationToken.None)
            .ConfigureAwait(false);
    }

    private async Task<IEnumerable<GraphNode>> ExtractDependenciesForSymbolAsync(SymbolNode symbol)
    {
        return await _roslynAnalyzer
            .ExtractDependenciesAsync(symbol, CancellationToken.None)
            .ConfigureAwait(false);
    }
}

