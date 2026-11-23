using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class KdTreeController : MonoBehaviour
{
    public InputActionReference toggleCells;
    public int maxLeafSize = 4;
    public int maxDepth = 24;
    public bool showCells = true;

    private KdTree tree;
    private GameObject visualizationRoot;
    private Mesh wireMesh;

    // Read-only access so the raycast controller can query the completed tree.
    public KdTree Tree => tree;

    private void Start()
    {
        var triangles = new List<Tri>();

        foreach (var meshFilter in FindObjectsByType<MeshFilter>())
        {
            var mesh = meshFilter.sharedMesh;
            if (mesh == null)
                continue;

            if (!mesh.isReadable)
            {
                Debug.LogWarning(
                    $"Skipping unreadable mesh '{mesh.name}' on '{meshFilter.name}'.",
                    meshFilter);
                continue;
            }

            var vertices = mesh.vertices;
            var indices = mesh.triangles;

            for (int i = 0; i < indices.Length; i += 3)
            {
                triangles.Add(new Tri
                {
                    a = meshFilter.transform.TransformPoint(vertices[indices[i]]),
                    b = meshFilter.transform.TransformPoint(vertices[indices[i + 1]]),
                    c = meshFilter.transform.TransformPoint(vertices[indices[i + 2]])
                });
            }
        }

        tree = new KdTree();
        tree.Build(triangles, maxLeafSize, maxDepth);

        CreateVisualization();
        if (visualizationRoot != null)
            visualizationRoot.SetActive(showCells);
    }

    private void Update()
    {
        bool toggled = toggleCells != null &&
                       toggleCells.action != null &&
                       toggleCells.action.triggered;

        if (!toggled &&
            Keyboard.current != null &&
            Keyboard.current.kKey.wasPressedThisFrame)
        {
            toggled = true;
        }

        if (toggled)
        {
            showCells = !showCells;
            if (visualizationRoot != null)
                visualizationRoot.SetActive(showCells);
        }
    }

    private void OnDestroy()
    {
        if (visualizationRoot != null)
            Destroy(visualizationRoot);
        if (wireMesh != null)
            Destroy(wireMesh);
    }

    private void CreateVisualization()
    {
        if (tree == null || tree.root == null)
            return;

        visualizationRoot = new GameObject("KD-Tree Visualization");
        wireMesh = CreateTreeMesh(tree.root);
        CreateWireMesh("KD-Tree Cells", wireMesh);
    }

    private Mesh CreateTreeMesh(KdNode node)
    {
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        CollectNodeEdges(node, vertices, indices);

        var mesh = new Mesh { name = "KD-Tree Wireframe" };
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
        mesh.UploadMeshData(false);
        return mesh;
    }

    private void CollectNodeEdges(
        KdNode node,
        List<Vector3> vertices,
        List<int> indices)
    {
        if (node == null)
            return;

        AddBox(node.box, vertices, indices);
        CollectNodeEdges(node.left, vertices, indices);
        CollectNodeEdges(node.right, vertices, indices);
    }

    private void AddBox(
        Bounds bounds,
        List<Vector3> vertices,
        List<int> indices)
    {
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;

        Vector3 p000 = c + new Vector3(-e.x, -e.y, -e.z);
        Vector3 p001 = c + new Vector3(-e.x, -e.y, e.z);
        Vector3 p010 = c + new Vector3(-e.x, e.y, -e.z);
        Vector3 p011 = c + new Vector3(-e.x, e.y, e.z);
        Vector3 p100 = c + new Vector3(e.x, -e.y, -e.z);
        Vector3 p101 = c + new Vector3(e.x, -e.y, e.z);
        Vector3 p110 = c + new Vector3(e.x, e.y, -e.z);
        Vector3 p111 = c + new Vector3(e.x, e.y, e.z);

        AddEdge(p000, p001, vertices, indices);
        AddEdge(p000, p010, vertices, indices);
        AddEdge(p000, p100, vertices, indices);

        AddEdge(p111, p110, vertices, indices);
        AddEdge(p111, p101, vertices, indices);
        AddEdge(p111, p011, vertices, indices);

        AddEdge(p001, p011, vertices, indices);
        AddEdge(p001, p101, vertices, indices);
        AddEdge(p010, p011, vertices, indices);
        AddEdge(p010, p110, vertices, indices);
        AddEdge(p100, p101, vertices, indices);
        AddEdge(p100, p110, vertices, indices);
    }

    private void AddEdge(
        Vector3 start,
        Vector3 end,
        List<Vector3> vertices,
        List<int> indices)
    {
        int startIndex = vertices.Count;
        vertices.Add(start);
        vertices.Add(end);

        indices.Add(startIndex);
        indices.Add(startIndex + 1);
    }

    private void CreateWireMesh(string name, Mesh mesh)
    {
        var objectWithMesh = new GameObject(name);
        objectWithMesh.transform.SetParent(visualizationRoot.transform, false);
        objectWithMesh.AddComponent<MeshFilter>().sharedMesh = mesh;

        var renderer = objectWithMesh.AddComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = Color.green;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
