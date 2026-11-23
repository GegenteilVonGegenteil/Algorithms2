using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class RaycastController : MonoBehaviour
{
    [SerializeField] private Camera rayCamera;
    [SerializeField] private KdTreeController treeController;
    [SerializeField] private InputActionReference castRay;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.002f;
    [SerializeField, Min(0f)] private float debugRayDuration = 0.75f;

    private GameObject hitVisualization;
    private Mesh hitMesh;
    private Material hitMaterial;

    private void Awake()
    {
        if (rayCamera == null)
            rayCamera = GetComponent<Camera>() ?? Camera.main;

        if (treeController == null)
            treeController = FindAnyObjectByType<KdTreeController>();
    }

    private void Update()
    {
        bool requested = castRay != null &&
                         castRay.action != null &&
                         castRay.action.triggered;

        if (!requested && Mouse.current != null)
            requested = Mouse.current.leftButton.wasPressedThisFrame;

        if (!requested && Keyboard.current != null)
            requested = Keyboard.current.spaceKey.wasPressedThisFrame;

        if (requested)
            CastFromViewCenter();
    }

    public bool CastFromViewCenter()
    {
        if (rayCamera == null || treeController == null || treeController.Tree == null)
            return false;

        RayData ray = new RayData(
            rayCamera.transform.position,
            rayCamera.transform.forward);

        // Let the k-d tree calculate the closest triangle hit.
        bool didHit = treeController.Tree.Raycast(
            ray,
            rayCamera.farClipPlane,
            out Tri triangle,
            out float distance);

        Debug.DrawRay(
            ray.origin,
            ray.direction * (didHit ? distance : rayCamera.farClipPlane),
            didHit ? hitColor : Color.yellow,
            debugRayDuration);

        if (didHit)
            ShowHit(triangle);
        else
            ClearHit();

        return didHit;
    }

    private void ShowHit(Tri triangle)
    {
        EnsureHitVisualization();

        Vector3 normal = Vector3.Cross(
            triangle.b - triangle.a,
            triangle.c - triangle.a).normalized;
        Vector3 offset = normal * surfaceOffset;

        // add points to the Triangle Mesh
        hitMesh.Clear();
        hitMesh.vertices = new[]
        {
            triangle.a + offset,
            triangle.b + offset,
            triangle.c + offset
        };
        hitMesh.triangles = new[] { 0, 1, 2, 2, 1, 0 };
        hitMesh.RecalculateNormals();
        hitMesh.RecalculateBounds();
        hitVisualization.SetActive(true); // set to visible
    }

    private void EnsureHitVisualization()
    {
        // create new Vis when there exists a hitVisualization 
        if (hitVisualization != null)
            return;

        // create Object, Mesh, Shader, Material and Renderer
        hitVisualization = new GameObject("Raycast Hit");
        hitMesh = new Mesh { name = "Raycast Hit Triangle" };
        hitVisualization.AddComponent<MeshFilter>().sharedMesh = hitMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        hitMaterial = new Material(shader)
        {
            name = "Raycast Hit Material",
            color = hitColor
        };

        // add MeshRenderer
        var meshRenderer = hitVisualization.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = hitMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void ClearHit()
    {
        if (hitVisualization != null)
            hitVisualization.SetActive(false);
    }

    private void OnDestroy()
    {
        if (hitVisualization != null)
            Destroy(hitVisualization);
        if (hitMesh != null)
            Destroy(hitMesh);
        if (hitMaterial != null)
            Destroy(hitMaterial);
    }
}
