using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace libESPER_V2.Utils
{
    internal class DAG
    {
        public List<Node> nodes = [];
        public DAG() { }
        
        public void AddNode(Node node)
        {
            nodes.Add(node);
        }
        public void AddNode(Node node, int index)
        {
            if (index < 0 || index > nodes.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            nodes.Insert(index, node);
        }
        public void DeleteNode(Node node)
        {
            nodes.Remove(node);
        }
        public void DeleteNode(int index)
        {
            if (index < 0 || index >= nodes.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            nodes.RemoveAt(index);
        }
        public void build(int distanceLimit, Func<long, long, double> distanceFunc)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].isRoot)
                {
                    continue;
                }
                int limit = Math.Max(0, i - distanceLimit);
                for (int j = i - 1; j >= limit; j--)
                {
                    double distance = distanceFunc(nodes[i].id, nodes[j].id);
                    if (nodes[j].value + distance < nodes[i].value)
                    {
                        nodes[i].parent = nodes[j];
                    }
                }
            }
        }
        public List<long> trace()
        {
            int currentIndex = nodes.Count - 1;
            int startIndex = nodes.Count - 1;
            double startValue = nodes[startIndex].value;
            while (nodes[currentIndex].isLeaf)
            {
                if (nodes[currentIndex].value < startValue)
                {
                    startValue = nodes[currentIndex].value;
                    startIndex = currentIndex;
                }
            }
            List<long> path = new();
            Node currentNode = nodes[startIndex];
            while (!currentNode.isRoot)
            {
                path.Add(currentNode.id);
                currentNode = currentNode.parent;
            }
            path.Add(currentNode.id);
            path.Reverse();
            return path;
        }
    }
    public class Node
    {
        public long id;
        public double value;
        public Node? parent;
        public bool isRoot;
        public bool isLeaf;

        public Node(bool isRoot, bool isLeaf)
        {
            this.value = 0;
            this.parent = null;
            this.isRoot = isRoot;
            this.isLeaf = isLeaf;
        }
        public Node(double value, Node parent, bool isRoot, bool isLeaf)
        {
            this.value = value;
            this.parent = parent;
            this.isRoot = isRoot;
            this.isLeaf = isLeaf;
        }
    }
}
