namespace ContextExtrator.Domain.Models;

public record FileTreeNode(FileNode Node)
{
    public List<FileTreeNode> Children { get; } = new();
}
