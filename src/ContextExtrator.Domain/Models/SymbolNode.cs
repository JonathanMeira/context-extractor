namespace ContextExtrator.Domain.Models;

public class SymbolNode
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Parent { get; set; } = string.Empty;
}
