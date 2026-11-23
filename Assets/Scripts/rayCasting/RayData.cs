using UnityEngine;

public struct RayData
{
    public Vector3 origin;
    public Vector3 direction;

    public RayData(Vector3 origin, Vector3 direction)
    {
        this.origin = origin;
        this.direction = direction.normalized;
    }
}
