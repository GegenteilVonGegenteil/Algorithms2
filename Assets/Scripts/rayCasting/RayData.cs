using UnityEngine;

public struct RayData
{
    // Only store the values needed by the custom raycast algorithm.
    public Vector3 origin;
    public Vector3 direction;

    public RayData(Vector3 origin, Vector3 direction)
    {
        this.origin = origin;
        this.direction = direction.normalized;
    }
}
