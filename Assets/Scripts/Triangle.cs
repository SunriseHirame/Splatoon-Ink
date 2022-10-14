using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Triangle
{
    public int Index;
    public Vector3 Vertex1;
    public Vector3 Vertex2;
    public Vector3 Vertex3;
    public Vector3 Normal => Vector3.Cross (Vertex1 - Vertex2, Vertex1 - Vertex3).normalized;

    public void AppendToListFiltered (List<Vector3> list, Vector3 origin, float distance)
    {
        var sqrDistance = distance * distance;

        if (Vector3.SqrMagnitude (Vertex1 - origin) < sqrDistance) list.Add (Vertex1);
        if (Vector3.SqrMagnitude (Vertex2 - origin) < sqrDistance) list.Add (Vertex2);
        if (Vector3.SqrMagnitude (Vertex3 - origin) < sqrDistance) list.Add (Vertex3);
    }

    public void AppendVertsTo (HashSet<Vector3> hashSet)
    {
        hashSet.Add (Vertex1);
        hashSet.Add (Vertex2);
        hashSet.Add (Vertex3);
    }

    public float SqrDistanceToTriangle (Vector3 point)
    {
        var closestPoint = ClosesPointOnTriangle (point);
        var sqrDistance = Vector3.SqrMagnitude (closestPoint - point);
        return sqrDistance;
    }

    public Vector3 ClosesPointOnTriangle (Vector3 sourcePosition)
    {
        var edge0 = Vertex2 - Vertex1;
        var edge1 = Vertex3 - Vertex1;
        var v0 = Vertex1 - sourcePosition;

        var a = Vector3.Dot (edge0, edge0); //edge0.dot( edge0 ));
        var b = Vector3.Dot (edge0, edge1); //.dot( edge1 );
        var c = Vector3.Dot (edge1, edge1); //.dot( edge1 );
        var d = Vector3.Dot (edge0, v0); //.dot( v0 );
        var e = Vector3.Dot (edge1, v0); //.dot( v0 );

        var det = a * c - b * b;
        var s = b * e - c * d;
        var t = b * d - a * e;

        if (s + t < det)
        {
            if (s < 0f)
            {
                if (t < 0f)
                {
                    if (d < 0f)
                    {
                        s = Mathf.Clamp (-d / a, 0f, 1f);
                        t = 0f;
                    }
                    else
                    {
                        s = 0f;
                        t = Mathf.Clamp (-e / c, 0f, 1f);
                    }
                }
                else
                {
                    s = 0f;
                    t = Mathf.Clamp (-e / c, 0f, 1f);
                }
            }
            else if (t < 0f)
            {
                s = Mathf.Clamp (-d / a, 0f, 1f);
                t = 0f;
            }
            else
            {
                float invDet = 1f / det;
                s *= invDet;
                t *= invDet;
            }
        }
        else
        {
            if (s < 0f)
            {
                var tmp0 = b + d;
                var tmp1 = c + e;
                if (tmp1 > tmp0)
                {
                    Debug.Log (6);

                    var numer = tmp1 - tmp0;
                    var denom = a - 2 * b + c;
                    s = Mathf.Clamp (numer / denom, 0f, 1f);
                    t = 1 - s;
                }
                else
                {
                    t = Mathf.Clamp (-e / c, 0f, 1f);
                    s = 0.0f;
                }
            }
            else if (t < 0f)
            {
                if (a + d > b + e)
                {
                    var numer = c + e - b - d;
                    var denom = a - 2 * b + c;
                    s = Mathf.Clamp (numer / denom, 0f, 1f);
                    t = 1 - s;
                }
                else
                {
                    s = 1f - Mathf.Clamp (-e / c, 0f, 1f);
                    t = 0f;
                }
            }
            else
            {
                var numer = c + e - b - d;
                var denom = a - 2 * b + c;
                s = Mathf.Clamp (numer / denom, 0f, 1f);
                t = 1f - s;
            }
        }

        return Vertex1 + s * edge0 + t * edge1;
    }
    
    const float EPSILON = 1e-8f;

    public bool Intersect (Ray localSpaceRay, out float distance)
    {
        // Triangle intersection code from http://three-eyed-games.com/2019/03/18/gpu-path-tracing-in-unity-part-3/
        distance = float.MaxValue;

        // find vectors for two edges sharing vert0
        var edge1 = Vertex2 - Vertex1;
        var edge2 = Vertex3 - Vertex1;

        // begin calculating determinant - also used to calculate U parameter
        var pVec = Vector3.Cross (localSpaceRay.direction, edge2);

        // if determinant is near zero, ray lies in plane of triangle
        var det = Vector3.Dot (edge1, pVec);

        // use backface culling
        if (det < EPSILON) return false;
        var invDet = 1.0f / det;

        // calculate distance from vert0 to ray origin
        var tVec = localSpaceRay.origin - Vertex1;

        // calculate U parameter and test bounds
        var u = Vector3.Dot (tVec, pVec) * invDet;
        if (u < 0.0 || u > 1.0f) return false;

        // prepare to test V parameter
        var qVec = Vector3.Cross (tVec, edge1);

        // calculate V parameter and test bounds
        var v = Vector3.Dot (localSpaceRay.direction, qVec) * invDet;
        if (v < 0.0 || u + v > 1.0f) return false;

        // calculate distance (t), ray intersects triangle
        distance = Vector3.Dot (edge2, qVec) * invDet;
        return true;
    }
}