namespace libESPER_V2.Utils
{
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
            int currentIndex = Nodes.Count - 1;
            int startIndex = Nodes.Count - 1;
            double startValue = Nodes[startIndex].Value;
            while (Nodes[currentIndex].IsLeaf)
            {
                if (Nodes[currentIndex].Value < startValue)
                {
                    startValue = Nodes[currentIndex].Value;
                    startIndex = currentIndex;
                }
            }
            List<int> path = new();
            if (Nodes.Count == 0)
                return path;
            Node currentNode = Nodes[startIndex];
            while (!currentNode.IsRoot)
            {
                path.Add(currentNode.Id);
                if (currentNode.Parent == null)
                    break;
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
        public double Value = 0;
        public Node? Parent;
        public bool IsRoot = isRoot;
        public bool IsLeaf = isLeaf;
    }
}
