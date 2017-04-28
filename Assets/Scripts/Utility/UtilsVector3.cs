using UnityEngine;
using System.Collections;

public static class UtilsVector3  {

    /// <summary>
    /// Returns the center of the sphere/capsulecast hit
    /// </summary>
    /// <param name="hit">The shapecast hit</param>
    /// <param name="origin">Where the cast was sent from</param>
    /// <param name="direction">The direction of the cast</param>
    /// <returns>The vector to the center of the hit from the origin</returns>
    public static Vector3 ShapecastCenter(this RaycastHit hit, Vector3 origin, Vector3 direction)
	{
		return origin + direction.normalized * hit.distance;
	}

// Based on Tinus' reply at forum.unity3d.com/threads/need-vector3-angle-to-return-a-negtive-or-relative-value.51092/
    public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 normal)
    {
        var angle = Vector3.Angle(v1, v2);
        var cross = Vector3.Cross(v1, v2);
        return cross.y >= 0 ? angle : -angle;
    }

    /// <summary>
    /// Returns a vector of the same length rotated onto a plane, as defined by the plane's normal
    /// </summary>
    /// <param name="vector">The vector to rotate</param>
    /// <param name="normal">The normal of the plane</param>
    /// <returns>A new vector of the same length, rotated to be parallel to the plane.</returns>
    public static Vector3 RotatedToPlane(this Vector3 vector, Vector3 normal)
    {
        var magnitude = vector.magnitude;
        return Vector3.ProjectOnPlane(vector, normal).normalized * magnitude;
    }

    /// <summary>
    /// Rotates a vector onto a plane
    /// </summary>
    /// <param name="vector">The vector to rotate</param>
    /// <param name="normal">The plane's normal</param>
    public static void RotateVectorParallelTo(ref Vector3 vector, Vector3 normal)
    {
        normal.RotateVectorToPlane(ref vector);
    }

    /// <summary>
    /// Rotates a vector onto a plane
    /// </summary>
    /// <param name="normal">The plane's normal</param>
    /// <param name="vector">The vector to rotate</param>
    public static void RotateVectorToPlane(this Vector3 normal, ref Vector3 vector)
    {
        var magnitude = vector.magnitude;
        vector = Vector3.ProjectOnPlane(vector, normal).normalized * magnitude;
    }
}