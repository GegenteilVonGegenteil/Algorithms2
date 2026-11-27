using System.Collections.Generic;
using UnityEngine;

// class representing a node in the kd-tree
public class KdTree
{
    // starting point of the tree, which contains all triangles and is split recursively into child nodes
    public KdNode root;
    // maximum number of triangles allowed in a leaf node before it is split into child nodes
    private int maxLeafSize;
    // maximum depth of the tree, which limits how many times a node can be split into child nodes
    private int maxDepth;

    // builds the kd-tree from a list of triangles, with optional parameters for maximum leaf size and maximum depth
    public void Build(List<Tri> tris, int maxLeafSize = 4, int maxDepth = 24)
    {
        this.maxLeafSize = Mathf.Max(1, maxLeafSize);
        this.maxDepth = Mathf.Max(1, maxDepth);
        root = tris == null || tris.Count == 0
            ? null
            : BuildNode(tris, 0);
    }

    // recursively builds a node in the kd-tree from a list of triangles and its depth in the tree
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

        Vector3 leftCenter = (sorted[split - 1].a + sorted[split - 1].b + sorted[split - 1].c) / 3f;
        Vector3 rightCenter = (sorted[split].a + sorted[split].b + sorted[split].c) / 3f;

        // Save the split plane because the ray traversal needs it later.
        node.splitAxis = axis;
        node.splitValue = (leftCenter[axis] + rightCenter[axis]) * 0.5f;

        // Add a triangle to every side of the split plane that it touches.
        var leftTris = new List<Tri>();
        var rightTris = new List<Tri>();
        foreach (Tri tri in tris)
        {
            float minimum = Mathf.Min(tri.a[axis], tri.b[axis], tri.c[axis]);
            float maximum = Mathf.Max(tri.a[axis], tri.b[axis], tri.c[axis]);

            // A triangle crossing the plane has to be stored in both children.
            if (minimum <= node.splitValue)
                leftTris.Add(tri);
            if (maximum >= node.splitValue)
                rightTris.Add(tri);
        }

        // Stop if the split would copy the complete list into both children again.
        if (leftTris.Count == tris.Count && rightTris.Count == tris.Count)
            return node;

        node.left = BuildNode(leftTris, depth + 1);
        node.right = BuildNode(rightTris, depth + 1);

        node.tris.Clear();
        
        return node;
    }

    public bool Raycast(RayData ray, float maxDistance, out Tri hit, out float t)
    {
        // Start without a hit; the first real distance is always smaller than infinity.
        hit = default;
        t = float.PositiveInfinity;

        if (root == null)
            return false;

        // Start the slide-based traversal with the complete ray segment.
        return RaycastNode(
            root,
            ray.origin,
            ray.direction,
            maxDistance,
            0f,
            ref hit,
            ref t);
    }

    private bool RaycastNode(
        KdNode node,
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        float distanceFromStart,
        ref Tri hit,
        ref float closest)
    {
        // Skip invalid segments and branches that start behind the closest hit.
        if (node == null || maxDistance < 0f || distanceFromStart > closest)
            return false;

        bool found = false;
        if (node.IsLeaf)
        {
            // Test the triangles stored in this leaf against the current ray segment.
            RayData ray = new RayData(origin, direction);
            foreach (Tri tri in node.tris)
            {
                // Keep only hits inside this segment and closer than the previous hit.
                if (RayTriangle(ray, tri, out float distance) &&
                    distance <= maxDistance &&
                    distanceFromStart + distance < closest)
                {
                    closest = distanceFromStart + distance;
                    hit = tri;
                    found = true;
                }
            }

            return found;
        }

        // Visit the child containing the segment origin first.
        int axis = node.splitAxis;
        bool startsOnRight = origin[axis] > node.splitValue;
        KdNode nearChild = startsOnRight ? node.right : node.left;
        KdNode farChild = startsOnRight ? node.left : node.right;

        // A ray parallel to the split plane stays in the same child.
        if (Mathf.Abs(direction[axis]) < 0.000001f)
            return RaycastNode(
                nearChild,
                origin,
                direction,
                maxDistance,
                distanceFromStart,
                ref hit,
                ref closest);

        // Calculate t where the ray reaches the split plane.
        float splitDistance = (node.splitValue - origin[axis]) / direction[axis];
        if (splitDistance >= 0f && splitDistance < maxDistance)
        {
            // Check the near segment up to the split plane first.
            found = RaycastNode(
                nearChild,
                origin,
                direction,
                splitDistance,
                distanceFromStart,
                ref hit,
                ref closest);

            // Check the far segment only if it can still contain a closer hit.
            if (distanceFromStart + splitDistance < closest)
            {
                found = RaycastNode(
                    farChild,
                    origin + direction * splitDistance,
                    direction,
                    maxDistance - splitDistance,
                    distanceFromStart + splitDistance,
                    ref hit,
                    ref closest) || found;
            }

            return found;
        }

        // The segment does not cross the plane, so only check its current side.
        return RaycastNode(
            nearChild,
            origin,
            direction,
            maxDistance,
            distanceFromStart,
            ref hit,
            ref closest);
    }

    // Triangle test, using RayData instead of Unity's Ray type.
    private static bool RayTriangle(RayData ray, Tri tri, out float distance)
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
