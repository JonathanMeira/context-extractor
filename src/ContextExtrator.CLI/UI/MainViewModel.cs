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
            .Where(f => f != null)
            .SelectMany(selectedProject => Observable.FromAsync(async () => await EnumerateProjectFilesAsync(selectedProject!)))
            .Subscribe()
            .DisposeWith(Subscriptions);

        // When file selection changes, extract symbols for that file (only if a project is selected)
        SelectedFileChanged
            .Where(f => f != null && SelectedProjectFile != null)
            .Do(_ => 
            {
                // Clear dependent data
                SelectedSymbol = null;
                Dependencies = new List<GraphNode>();
            })
            .SelectMany(file => ExtractSymbolsForFile(file!, _analysisCancellation?.Token ?? CancellationToken.None))
            .Subscribe(symbols => Symbols = symbols.ToList())
            .DisposeWith(Subscriptions);

        // When symbol selection changes, extract dependencies for that symbol (only if a project is selected)
        SelectedSymbolChanged
            .Where(s => s != null && SelectedProjectFile != null)
            .SelectMany(symbol => ExtractDependenciesForSymbol(symbol!, _analysisCancellation?.Token ?? CancellationToken.None))
            .Subscribe(deps => Dependencies = deps.ToList())
            .DisposeWith(Subscriptions);
    }

    public void CancelAnalysis()
    {
        _analysisCancellation?.Cancel();
        IsAnalyzing = false;
    }

    private async Task EnumerateProjectFilesAsync(FileNode project)
    {
        SelectedFile = null;
        Symbols = [];
        SelectedSymbol = null;
        Dependencies = [];

        IEnumerable<FileTreeNode> tree = await _discoveryService.EnumerateFileTreeAsync(project.Path, CancellationToken.None).ConfigureAwait(false);
        FileTree = [.. tree];
    }

    private async Task<IEnumerable<SymbolNode>> ExtractSymbolsForFile(FileNode file, CancellationToken ct)
    {
        try
        {
            var symbols = await _roslynAnalyzer.ExtractSymbolsAsync(file.Path, ct).ConfigureAwait(false);
            return symbols;
        }
        catch
        {
            return Enumerable.Empty<SymbolNode>();
        }
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

