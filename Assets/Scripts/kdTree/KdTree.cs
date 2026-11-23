using System.Collections.Generic;
using UnityEngine;

public class KdTree
{
    public KdNode root;
    private int maxLeafSize;
    private int maxDepth;

    public void Build(List<Tri> tris, int maxLeafSize = 4, int maxDepth = 24)
    {
        this.maxLeafSize = Mathf.Max(1, maxLeafSize);
        this.maxDepth = Mathf.Max(1, maxDepth);
        root = tris == null || tris.Count == 0
            ? null
            : BuildNode(tris, 0);
    }

    private KdNode BuildNode(List<Tri> tris, int depth)
    {
        var node = new KdNode(tris, depth);

        if (tris.Count <= maxLeafSize || depth >= maxDepth)
            return node;

        int axis = 0;
        if (node.box.size.y > node.box.size.x)
            axis = 1;
        if (node.box.size.z > node.box.size[axis])
            axis = 2;

        var sorted = new List<Tri>(tris);
        sorted.Sort((first, second) =>
        {
            Vector3 firstCenter = (first.a + first.b + first.c) / 3f;
            Vector3 secondCenter = (second.a + second.b + second.c) / 3f;
            return firstCenter[axis].CompareTo(secondCenter[axis]);
        });

        int split = sorted.Count / 2;
        if (split == 0 || split == sorted.Count)
            return node;

        var leftTris = sorted.GetRange(0, split);
        var rightTris = sorted.GetRange(split, sorted.Count - split);

        Vector3 leftCenter = (sorted[split - 1].a + sorted[split - 1].b + sorted[split - 1].c) / 3f;
        Vector3 rightCenter = (sorted[split].a + sorted[split].b + sorted[split].c) / 3f;
        node.splitAxis = axis;
        node.splitValue = (leftCenter[axis] + rightCenter[axis]) * 0.5f;

        node.left = BuildNode(leftTris, depth + 1);
        node.right = BuildNode(rightTris, depth + 1);

        node.tris.Clear();
        
        return node;
    }

    public bool Raycast(Ray ray, out Tri hit, out float t)
    {
        hit = default;
        t = float.PositiveInfinity;

        if (root == null)
            return false;

        bool found = RaycastNode(root, ray, ref hit, ref t);
        return found;
    }

    private bool RaycastNode(KdNode node, Ray ray, ref Tri hit, ref float closest)
    {
        if (!node.box.IntersectRay(ray, out float boxDistance) || boxDistance > closest)
            return false;

        bool found = false;
        if (node.IsLeaf)
        {
            foreach (Tri tri in node.tris)
            {
                if (RayTriangle(ray, tri, out float distance) && distance >= 0f && distance < closest)
                {
                    closest = distance;
                    hit = tri;
                    found = true;
                }
            }
            return found;
        }

        float leftDistance = node.left.box.IntersectRay(ray, out float leftBoxDistance)
            ? leftBoxDistance
            : float.PositiveInfinity;
        float rightDistance = node.right.box.IntersectRay(ray, out float rightBoxDistance)
            ? rightBoxDistance
            : float.PositiveInfinity;

        if (leftDistance <= rightDistance)
        {
            found = RaycastNode(node.left, ray, ref hit, ref closest) || found;
            found = RaycastNode(node.right, ray, ref hit, ref closest) || found;
        }
        else
        {
            found = RaycastNode(node.right, ray, ref hit, ref closest) || found;
            found = RaycastNode(node.left, ray, ref hit, ref closest) || found;
        }

        return found;
    }

    private static bool RayTriangle(Ray ray, Tri tri, out float distance)
    {
        Vector3 edge1 = tri.b - tri.a;
        Vector3 edge2 = tri.c - tri.a;
        Vector3 p = Vector3.Cross(ray.direction, edge2);
        float determinant = Vector3.Dot(edge1, p);
        distance = 0f;

        if (Mathf.Abs(determinant) < 0.000001f)
            return false;

        float inverse = 1f / determinant;
        Vector3 t = ray.origin - tri.a;
        float u = Vector3.Dot(t, p) * inverse;
        if (u < 0f || u > 1f)
            return false;

        Vector3 q = Vector3.Cross(t, edge1);
        float v = Vector3.Dot(ray.direction, q) * inverse;
        if (v < 0f || u + v > 1f)
            return false;

        distance = Vector3.Dot(edge2, q) * inverse;
        return distance >= 0f;
    }
}
