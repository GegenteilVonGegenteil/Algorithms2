using System.Collections.Generic;
using UnityEngine;

// class representing a node in the kd-tree
public class KdNode {
    // The bounding box of the node
    public Bounds box;
    // The left and right child nodes
    public KdNode left, right;
    // list of triangles contained in this node
    public List<Tri> tris;
    // depth of the node in the tree, which is used to determine the splitting axis and value
    public int depth;
    // axis along which the node is split (0 = x, 1 = y, 2 = z)
    public int splitAxis;
    // value along the split axis that defines the splitting plane
    public float splitValue;
    // property to check if the node is a leaf (i.e., has no children)
    public bool IsLeaf => left == null;

    // constructor for the KdNode class, which initializes the node with a list of triangles and its depth in the tree
    public KdNode(List<Tri> tris, int depth) {
        this.depth = depth;
        this.tris = tris;
        box = new Bounds();
        foreach (var tri in tris) {
            box.Encapsulate(tri.a);
            box.Encapsulate(tri.b);
            box.Encapsulate(tri.c);
        }
    }
}
