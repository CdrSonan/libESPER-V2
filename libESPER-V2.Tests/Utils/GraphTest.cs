using System;
using libESPER_V2.Utils;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace libESPER_V2.Tests.Utils;

[TestFixture]
[TestOf(typeof(Graph))]
public class GraphTest
{
    [SetUp]
    public void SetUp()
    {
        _dag = new Graph();
    }

    private Graph _dag;

    [Test]
    public void AddNode_ShouldAddNodeToList()
    {
        var node = new Node(1, false, true);
        _dag.AddNode(node);

        ClassicAssert.AreEqual(1, _dag.Nodes.Count);
        ClassicAssert.AreSame(node, _dag.Nodes[0]);
    }

    [Test]
    public void AddNode_WithIndex_ShouldInsertNodeAtCorrectIndex()
    {
        var node1 = new Node(1, false, true);
        var node2 = new Node(2, false, true);
        _dag.AddNode(node1);
        _dag.AddNode(node2, 0);

        ClassicAssert.AreEqual(2, _dag.Nodes.Count);
        ClassicAssert.AreSame(node2, _dag.Nodes[0]);
        ClassicAssert.AreSame(node1, _dag.Nodes[1]);
    }

    [Test]
    public void AddNode_WithInvalidIndex_ShouldThrowArgumentOutOfRangeException()
    {
        var node = new Node(1, false, true);

        Assert.Throws<ArgumentOutOfRangeException>(() => _dag.AddNode(node, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _dag.AddNode(node, 1));
    }

    [Test]
    public void DeleteNode_ByNode_ShouldRemoveNodeFromList()
    {
        var node1 = new Node(1, false, true);
        var node2 = new Node(2, false, true);
        _dag.AddNode(node1);
        _dag.AddNode(node2);

        _dag.DeleteNode(node1);

        ClassicAssert.AreEqual(1, _dag.Nodes.Count);
        ClassicAssert.AreSame(node2, _dag.Nodes[0]);
    }

    [Test]
    public void DeleteNode_ByNode_ShouldHandleNonexistentNodeGracefully()
    {
        var node1 = new Node(1, false, true);
        var node2 = new Node(2, false, true);
        _dag.AddNode(node1);

        _dag.DeleteNode(node2);

        ClassicAssert.AreEqual(1, _dag.Nodes.Count);
        ClassicAssert.AreSame(node1, _dag.Nodes[0]);
    }

    [Test]
    public void DeleteNode_ByIndex_ShouldRemoveNodeAtIndex()
    {
        var node1 = new Node(1, false, true);
        var node2 = new Node(2, false, true);
        _dag.AddNode(node1);
        _dag.AddNode(node2);

        _dag.DeleteNode(0);

        ClassicAssert.AreEqual(1, _dag.Nodes.Count);
        ClassicAssert.AreSame(node2, _dag.Nodes[0]);
    }

    [Test]
    public void DeleteNode_WithInvalidIndex_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _dag.DeleteNode(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _dag.DeleteNode(0));
    }

    [Test]
    public void Trace_WithEmptyDag_ShouldReturnEmptyPath()
    {
        var result = _dag.Trace();

        ClassicAssert.IsNotNull(result);
        ClassicAssert.IsEmpty(result);
    }

    [Test]
    public void Trace_WithSingleNodeDag_ShouldReturnPathWithSingleNode()
    {
        var node = new Node(1, true, true);
        _dag.AddNode(node);

        var result = _dag.Trace();

        ClassicAssert.AreEqual(1, result.Count);
        ClassicAssert.AreEqual(1, result[0]);
    }

    [Test]
    public void Trace_WithMultipleNodes_ShouldReturnCorrectPath()
    {
        var root = new Node(1, true, false) { Value = 2 };
        var leaf1 = new Node(2, false, true) { Value = 5, Parent = root };
        var leaf2 = new Node(3, false, true) { Value = 4, Parent = root };
        var leaf3 = new Node(4, false, true) { Value = 8, Parent = root };

        _dag.AddNode(root);
        _dag.AddNode(leaf1);
        _dag.AddNode(leaf2);
        _dag.AddNode(leaf3);

        var result = _dag.Trace();

        ClassicAssert.AreEqual(2, result.Count);
        ClassicAssert.Contains(1, result);
        ClassicAssert.Contains(3, result); // Leaf with the smaller value
    }

    [Test]
    public void Trace_WithNoLeafNodes_ShouldReturnEmptyPath()
    {
        var root = new Node(1, true, false);
        _dag.AddNode(root);

        var result = _dag.Trace();

        ClassicAssert.IsEmpty(result);
    }

    [Test]
    public void Trace_WithNoRootNodes_ShouldReturnCorrectPath()
    {
        var node1 = new Node(1, false, false) { Value = 2 };
        var node2 = new Node(2, false, false) { Value = 5, Parent = node1 };
        var leaf = new Node(3, false, true) { Value = 8, Parent = node2 };
        _dag.AddNode(node1);
        _dag.AddNode(node2);
        _dag.AddNode(leaf);

        var result = _dag.Trace();

        ClassicAssert.AreEqual(3, result.Count);
    }
}