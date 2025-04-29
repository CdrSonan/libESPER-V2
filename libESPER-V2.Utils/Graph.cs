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
        public List<int> trace()
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
            List<int> path = new();
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
    public class Node(int id, bool isRoot, bool isLeaf)
    {
        public int id = id;
        public double value = 0;
        public Node? parent;
        public bool isRoot = isRoot;
        public bool isLeaf = isLeaf;
    }
}
