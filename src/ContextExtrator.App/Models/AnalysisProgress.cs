using ContextExtrator.Domain.Models;

public enum AnalysisPhase
{
    Scanning,
    Analyzing,
    BuildingGraph,
    Complete,
    Error
}

public class AnalysisProgress
{
    public int FilesProcessed { get; set; }
    public int TotalFiles { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public AnalysisPhase Phase { get; set; }
}

public class AnalysisResult
{
    public FileNode[] Files { get; set; } = System.Array.Empty<FileNode>();
    public SymbolNode[] Symbols { get; set; } = System.Array.Empty<SymbolNode>();
    public GraphNode[] Dependencies { get; set; } = System.Array.Empty<GraphNode>();
}

public class ProjectDescriptor
{
    public string Path { get; set; } = string.Empty;
}
