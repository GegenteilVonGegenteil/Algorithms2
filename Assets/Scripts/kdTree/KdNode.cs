using System.Collections.Generic;
using UnityEngine;

public class KdNode {
    public Bounds box;
    public KdNode left, right;
    public List<Tri> tris;
    public int depth;
    public int splitAxis;
    public float splitValue;
    public bool IsLeaf => left == null;

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
