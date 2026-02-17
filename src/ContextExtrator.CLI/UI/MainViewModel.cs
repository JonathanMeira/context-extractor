using ContextExtrator.Domain.Analysis;
using ContextExtrator.Domain.Models;
using System.Reactive.Linq;
using Termina.Reactive;

namespace ContextExtrator.CLI.UI;

public partial class MainViewModel : ReactiveViewModel
{
    private readonly IRoslynAnalyzer _roslynAnalyzer;
    private readonly IDiscoveryService _discoveryService;
    private CancellationTokenSource? _analysisCancellation;

    public MainViewModel(IRoslynAnalyzer roslynAnalyzer, IDiscoveryService discoveryService)
    {
        _roslynAnalyzer = roslynAnalyzer;
        _discoveryService = discoveryService;
    }

    [Reactive] private string _projectRoot = "E:\\workspaces\\ws-dotnet-2026\\context-extractor";

    [Reactive] private List<FileNode> _projectFiles = [];
    [Reactive] private FileNode? _selectedProjectFile;


    [Reactive] private List<FileNode> _files = [];
    [Reactive] private List<SymbolNode> _symbols = [];


    [Reactive] private int _filesProcessed;
    [Reactive] private int _filesScanned;
    [Reactive] private int _symbolsExtracted;
    [Reactive] private bool _isAnalyzing;
    [Reactive] private FileNode? _selectedFile;
    [Reactive] private SymbolNode? _selectedSymbol;
    [Reactive] private List<GraphNode> _dependencies = new();

    [Reactive] private List<Domain.Models.FileTreeNode> _fileTree = new();


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
            .Where(s => s != null && SelectedProjectFile != null)
            .SelectMany(symbol => ExtractDependenciesForSymbol(symbol!, _analysisCancellation?.Token ?? CancellationToken.None))
            .Subscribe(deps => Dependencies = [.. deps])
            .DisposeWith(Subscriptions);
    }

    public void CancelAnalysis()
    {
        _analysisCancellation?.Cancel();
        IsAnalyzing = false;
    }

    private async Task<IEnumerable<FileTreeNode>> EnumerateProjectFilesAsync(FileNode project)
    {
         return await _discoveryService
            .EnumerateFileTreeAsync(project.Path, CancellationToken.None)
            .ConfigureAwait(false);
        
        
    }

    private async Task<IEnumerable<SymbolNode>> ExtractSymbolsForFileAsync(FileNode file)
    {
        var symbols = await _roslynAnalyzer
            .ExtractSymbolsAsync(file.Path, CancellationToken.None)
            .ConfigureAwait(false);

        return symbols;
    }

    private async Task<IEnumerable<GraphNode>> ExtractDependenciesForSymbol(SymbolNode symbol, CancellationToken ct)
    {
        try
        {
            var deps = await _roslynAnalyzer.ExtractDependenciesAsync(symbol, ct).ConfigureAwait(false);
            return deps;
        }
        catch
        {
            return Enumerable.Empty<GraphNode>();
        }
    }


}

