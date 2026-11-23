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
        // Use the attached camera when no camera was assigned in the Inspector.
        if (rayCamera == null)
            rayCamera = GetComponent<Camera>() ?? Camera.main;

        // Find the scene's k-d tree controller when it was not assigned manually.
        if (treeController == null)
            treeController = FindAnyObjectByType<KdTreeController>();
    }

    private void Update()
    {
        // Prefer an optional configured input action.
        bool requested = castRay != null &&
                         castRay.action != null &&
                         castRay.action.triggered;

        // Also support the left mouse button and space bar directly.
        if (!requested && Mouse.current != null)
            requested = Mouse.current.leftButton.wasPressedThisFrame;

        if (!requested && Keyboard.current != null)
            requested = Keyboard.current.spaceKey.wasPressedThisFrame;

        // Cast one ray whenever one of the supported inputs is pressed.
        if (requested)
            CastFromViewCenter();
    }

    public bool CastFromViewCenter()
    {
        // The camera and a completed k-d tree are required before casting.
        if (rayCamera == null || treeController == null || treeController.Root == null)
            return false;

        // Define S(t) = camera position + t * camera forward direction.
        RayData ray = new RayData(
            rayCamera.transform.position,
            rayCamera.transform.forward);

        // Traverse the k-d tree only as far as the camera's far clipping plane.
        bool didHit = RaycastTree(
            treeController.Root,
            ray,
            rayCamera.farClipPlane,
            out Tri triangle,
            out float distance);
        // Draw a temporary debug ray: red for a hit and yellow for a miss.
        Debug.DrawRay(
            ray.origin,
            ray.direction * (didHit ? distance : rayCamera.farClipPlane),
            didHit ? hitColor : Color.yellow,
            debugRayDuration);

        // Display the nearest triangle or remove the previous hit visualization.
        if (didHit)
            ShowHit(triangle);
        else
            ClearHit();
        return didHit;
    }

    private bool RaycastTree(
        KdNode root,
        RayData ray,
        float maxDistance,
        out Tri hit,
        out float distance)
    {
        // Start without a hit and with no known closest distance.
        hit = default;
        distance = float.PositiveInfinity;

        // Traverse S(t) = origin + t * direction from the root node.
        return VisitNodes(
            root,
            ray.origin,
            ray.direction,
            maxDistance,
            0f,
            ref hit,
            ref distance);
    }

    private bool VisitNodes(
        KdNode node,
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        float distanceFromStart,
        ref Tri hit,
        ref float closest)
    {
        // Stop at missing nodes, exhausted segments, or cells behind a closer hit.
        if (node == null || maxDistance < 0f || distanceFromStart > closest)
            return false;

        bool found = false;

        // Leaf nodes contain the triangles that must be tested directly.
        if (node.IsLeaf)
        {
            RayData ray = new RayData(origin, direction);
            foreach (Tri triangle in node.tris)
            {
                // Accept only intersections inside this segment and before prior hits.
                if (RayTriangle(ray, triangle, out float localDistance) &&
                    localDistance <= maxDistance &&
                    distanceFromStart + localDistance < closest)
                {
                    closest = distanceFromStart + localDistance;
                    hit = triangle;
                    found = true;
                }
            }

            return found;
        }

        // Determine which child contains the current segment origin.
        int axis = node.splitAxis;
        bool startsOnRight = origin[axis] > node.splitValue;
        KdNode nearChild = startsOnRight ? node.right : node.left;
        KdNode farChild = startsOnRight ? node.left : node.right;

        // A segment parallel to the splitting plane stays on its current side.
        if (Mathf.Abs(direction[axis]) < 0.000001f)
            return VisitNodes(
                nearChild,
                origin,
                direction,
                maxDistance,
                distanceFromStart,
                ref hit,
                ref closest);

        // Solve origin[axis] + t * direction[axis] = splitValue.
        float splitDistance = (node.splitValue - origin[axis]) / direction[axis];

        // When the plane intersects this segment, visit near and then far.
        if (splitDistance >= 0f && splitDistance < maxDistance)
        {
            found = VisitNodes(
                nearChild,
                origin,
                direction,
                splitDistance,
                distanceFromStart,
                ref hit,
                ref closest);

            // Skip the far child if a closer hit was already found.
            if (distanceFromStart + splitDistance < closest)
            {
                found = VisitNodes(
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

        // The splitting plane is outside the segment, so visit only its current side.
        return VisitNodes(
            nearChild,
            origin,
            direction,
            maxDistance,
            distanceFromStart,
            ref hit,
            ref closest);
    }

    private static bool RayTriangle(RayData ray, Tri triangle, out float distance)
    {
        // Moller-Trumbore ray-triangle intersection.
        Vector3 edge1 = triangle.b - triangle.a;
        Vector3 edge2 = triangle.c - triangle.a;
        Vector3 p = Vector3.Cross(ray.direction, edge2);
        float determinant = Vector3.Dot(edge1, p);
        distance = 0f;

        // A near-zero determinant means the ray is parallel to the triangle.
        if (Mathf.Abs(determinant) < 0.000001f)
            return false;

        float inverse = 1f / determinant;
        Vector3 t = ray.origin - triangle.a;
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

    private void ShowHit(Tri triangle)
    {
        // Create the reusable visualization objects the first time they are needed.
        EnsureHitVisualization();

        // Calculate the triangle normal and offset the overlay to prevent z-fighting.
        Vector3 normal = Vector3.Cross(
            triangle.b - triangle.a,
            triangle.c - triangle.a).normalized;
        Vector3 offset = normal * surfaceOffset;

        // Replace the overlay mesh with the three vertices of the hit triangle.
        hitMesh.Clear();
        hitMesh.vertices = new[]
        {
            triangle.a + offset,
            triangle.b + offset,
            triangle.c + offset
        };
        // Add both winding orders so the highlighted triangle is visible from both sides.
        hitMesh.triangles = new[] { 0, 1, 2, 2, 1, 0 };

        // Update the mesh data and make the highlight visible.
        hitMesh.RecalculateNormals();
        hitMesh.RecalculateBounds();
        hitVisualization.SetActive(true);
    }

    private void EnsureHitVisualization()
    {
        // Reuse the existing overlay instead of allocating one for every cast.
        if (hitVisualization != null)
            return;

        // Create an object containing a dynamically updated triangle mesh.
        hitVisualization = new GameObject("Raycast Hit");
        hitMesh = new Mesh { name = "Raycast Hit Triangle" };
        hitVisualization.AddComponent<MeshFilter>().sharedMesh = hitMesh;

        // Prefer an unlit shader so the hit color remains clearly visible.
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        // Create the distinct material required for first-hit highlighting.
        hitMaterial = new Material(shader)
        {
            name = "Raycast Hit Material",
            color = hitColor
        };

        // Render the overlay without casting or receiving shadows.
        var meshRenderer = hitVisualization.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = hitMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void ClearHit()
    {
        // A miss hides the previous triangle highlight.
        if (hitVisualization != null)
            hitVisualization.SetActive(false);
    }

    private void OnDestroy()
    {
        // Release all runtime-created Unity objects when this component is destroyed.
        if (hitVisualization != null)
            Destroy(hitVisualization);
        if (hitMesh != null)
            Destroy(hitMesh);
        if (hitMaterial != null)
            Destroy(hitMaterial);
    }
}
