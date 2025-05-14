namespace libESPER_V2.Utils;

internal class Dag
{
    public List<Node> Nodes = [];

    public void AddNode(Node node)
    {
        Nodes.Add(node);
    }

    public void AddNode(Node node, int index)
    {
        if (index < 0 || index > Nodes.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        Nodes.Insert(index, node);
    }

    public void DeleteNode(Node node)
    {
        Nodes.Remove(node);
    }

    public void DeleteNode(int index)
    {
        if (index < 0 || index >= Nodes.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        Nodes.RemoveAt(index);
    }

    public List<int> Trace()
    {
        if (Nodes.Count == 0)
            return [];
        var currentIndex = Nodes.Count - 2;
        var startIndex = Nodes.Count - 1;
        var startValue = Nodes[startIndex].Value;
        while (currentIndex >= 0 && Nodes[currentIndex].IsLeaf)
        {
            if (Nodes[currentIndex].Value < startValue)
            {
                startValue = Nodes[currentIndex].Value;
                startIndex = currentIndex;
            }

            currentIndex--;
        }

        List<int> path = new();
        var currentNode = Nodes[startIndex];
        if (!currentNode.IsLeaf) return path;
        while (!currentNode.IsRoot && currentNode.Parent != null)
        {
            path.Add(currentNode.Id);
            currentNode = currentNode.Parent;
        }

        path.Add(currentNode.Id);
        path.Reverse();
        return path;
    }
}

public class Node(int id, bool isRoot, bool isLeaf)
{
    public int Id = id;
    public bool IsLeaf = isLeaf;
    public bool IsRoot = isRoot;
    public Node? Parent;
    public double Value = 0;
}