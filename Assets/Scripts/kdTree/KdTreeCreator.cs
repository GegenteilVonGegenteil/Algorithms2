using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// class that creates a kd-tree from all meshes in the scene and provides a visualization of the tree
public class KdTreeController : MonoBehaviour
{
    // input action reference for toggling the visualization of the kd-tree cells
    public InputActionReference toggleCells;

    // maximum number of triangles allowed in a leaf node before it is split into child nodes
    public int maxLeafSize = 4;
    // maximum depth of the tree, which limits how many times a node can be split into child nodes
    public int maxDepth = 24;
    // boolean to determine whether the visualization of the kd-tree cells should be shown or hidden
    public bool showCells = true;

    // private fields for the kd-tree, the root object of the visualization, and the wireframe mesh used for visualization
    private KdTree tree;
    private GameObject visualizationRoot;
    private Mesh wireMesh;

    // Read-only access so the raycast controller can query the completed tree.
    public KdTree Tree => tree;

    // initializes the kd-tree by collecting all triangles from meshes in the scene, building the tree, and creating a visualization of the tree
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

            // check if the meshes object is called "Floor" and skip it if so
            if (meshFilter.gameObject.name.ToLower().Contains("ground") || meshFilter.gameObject.name.ToLower().Contains("floor"))
            {
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

    // checks for input to toggle the visualization of the kd-tree cells
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
            ToggleVisualization();
    }

    // function to toggle the visualization of the kd-tree cells on or off
    public void ToggleVisualization()
    {
        // Switch the saved state and show or hide the complete wireframe object.
        showCells = !showCells;

        if (visualizationRoot != null)
            visualizationRoot.SetActive(showCells);
    }

    // creates a visualization of the kd-tree by generating a wireframe mesh for each node in the tree and adding it to the scene
    private void CreateVisualization()
    {
        if (tree == null || tree.root == null)
            return;

        visualizationRoot = new GameObject("KD-Tree Visualization");
        wireMesh = CreateTreeMesh(tree.root);
        CreateWireMesh("KD-Tree Cells", wireMesh);
    }

    // creates a wireframe mesh for the kd-tree by collecting the edges of each node's bounding box and adding them to a mesh
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

    // recursively collects the edges of each node's bounding box and adds them to a list of vertices and indices for the wireframe mesh
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

    // adds the edges of a bounding box to a list of vertices and indices for the wireframe mesh
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

    // adds an edge between two points to a list of vertices and indices for the wireframe mesh
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

    // creates a new GameObject with a MeshFilter and MeshRenderer to display the wireframe mesh in the scene
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
