namespace ContextExtrator.Domain.Models;

public class GraphNode
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string[] References { get; set; } = System.Array.Empty<string>();
}
