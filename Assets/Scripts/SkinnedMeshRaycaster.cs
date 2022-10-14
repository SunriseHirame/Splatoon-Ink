using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

[System.Serializable]
public class SkinnedMeshRaycaster : IDisposable
{
    [SerializeField] private MeshFilter _meshFilter;

    private SkinnedMeshRenderer _skinnedMeshRenderer;
    private Mesh _mesh;

    private List<Vector3> _meshVertices;
    private List<Vector3> _meshNormals;
    private int[] _meshTriangles;

    private Triangle[] _raycastTriangles;


    private List<Vector3> _lastIntersectingVertices = new List<Vector3>();
    private List<int> _lastIntersectingVertexIndices = new List<int>();

    private float[] _vertexFacingMask;

    private Dictionary<int, List<int>> _vertexTriangleLookUp = new Dictionary<int, List<int>>();

    public void SetTargetRenderer (SkinnedMeshRenderer skinnedMeshRenderer)
    {
        _skinnedMeshRenderer = skinnedMeshRenderer;
        _mesh = Object.Instantiate (_skinnedMeshRenderer.sharedMesh);

        _meshVertices = new List<Vector3> (_mesh.vertexCount);
        _mesh.GetVertices (_meshVertices);

        _meshNormals = new List<Vector3> (_mesh.vertexCount);
        _mesh.GetNormals (_meshNormals);
        
        _meshTriangles = _mesh.triangles;
        _raycastTriangles = new Triangle[_meshTriangles.Length / 3];

        _vertexFacingMask = new float[_mesh.vertexCount];
        
        BuildRaycastTriangles (true);
    }

    public bool Raycast (Ray ray, float radius, out Vector3 point)
    {
        point = default;
        
        if (!_mesh) return false;
        //if (_skinnedMeshRenderer.bounds.IntersectRay (ray)) return false;

        Profiler.BeginSample ("Paint");

        _lastIntersectingVertices.Clear ();

        _skinnedMeshRenderer.BakeMesh (_mesh);

        if (_meshFilter) _meshFilter.mesh = _mesh;

        _mesh.GetVertices (_meshVertices);
        _mesh.GetNormals (_meshNormals);

        var meshTransform = _skinnedMeshRenderer.transform;
        
        var meshSpaceRay = new Ray (
            meshTransform.InverseTransformPoint (ray.origin),
            meshTransform.InverseTransformDirection (ray.direction));

        Debug.DrawRay (meshSpaceRay.origin, meshSpaceRay.direction * 20f, Color.yellow);

        BuildRaycastTriangles (false);
        var firstTriangleHit = FindBestTriangleIntersection (meshSpaceRay, out var meshSpacePoint);
        var trisInRange = FindTrisInRange (meshSpacePoint, firstTriangleHit, radius, -meshSpaceRay.direction);

        for (int i = 0; i < _vertexFacingMask.Length; i++)
        {
            _vertexFacingMask[i] = 0;
        }

        var hashSet = new HashSet<Vector3> ();
        var vertHelper = new List<int> ();
        foreach (var tri in trisInRange)
        {
            tri.AppendVertsTo (hashSet);
            GetVertexIndexes (tri.Index, vertHelper);
            foreach (var helper in vertHelper)
            {
                _vertexFacingMask[helper] = 1f;
            }
        }

        _lastIntersectingVertices.Clear ();
        _lastIntersectingVertices.AddRange (hashSet);

        point = meshTransform.TransformPoint (meshSpacePoint);
        Debug.DrawRay (point, -ray.direction, Color.blue, 1f);
        
        Profiler.EndSample ();

        return true;
    }

    private readonly HashSet<Triangle> _triangleSetHelper = new HashSet<Triangle>();

    private void GetPotentialTriangles (List<int> vertexes, List<Triangle> triangles)
    {
        _triangleSetHelper.Clear ();

        foreach (var vertex in vertexes)
        {
            var triangleIndexList = _vertexTriangleLookUp[vertex];
            foreach (var triangleIndex in triangleIndexList)
            {
                _triangleSetHelper.Add (_raycastTriangles[triangleIndex]);
            }
        }

        triangles.AddRange (_triangleSetHelper);
    }

    private List<Triangle> FindTrisInRange (Vector3 point, Triangle startTriangle, float distance, Vector3 facing)
    {
        Profiler.BeginSample ("FindTrisInRange");
        // Use flood fill with cutoff distance to find triangles in range.
        // Include all triangles that are withing the range and have the required facing
        
        var vertexIndexBuffer = new List<int> ();
        var triBuffer = new List<Triangle> ();

        var visitedTris = new HashSet<int> ();

        var openSet = new Queue<Triangle> ();
        var inRange = new List<Triangle> ();

        openSet.Enqueue (startTriangle);
        visitedTris.Add (startTriangle.Index);
        
        inRange.Add (startTriangle);
        inRange.AddRange (GetNeighbours (startTriangle));

        // while (openSet.Count > 0)
        // {
        //     var triToCheck = openSet.Dequeue ();
        //     if (triToCheck.SqrDistanceToTriangle (point) > distance) continue;
        //
        //     inRange.Add (triToCheck);
        //
        //     vertexIndexBuffer.Clear ();
        //     GetVertexIndexes (triToCheck.Index, vertexIndexBuffer);
        //
        //     foreach (var vertexIndex in vertexIndexBuffer)
        //     {
        //         triBuffer.Clear ();
        //         GetTris (vertexIndex, triBuffer);
        //
        //         foreach (var tri in triBuffer)
        //         {
        //             if (visitedTris.Contains (tri.Index)) continue;
        //
        //             visitedTris.Add (tri.Index);
        //
        //             // Discard triangles that are facing away from the facing.
        //             var normal = tri.Normal;
        //             var dot = Vector3.Dot (facing, normal);
        //             if (dot < 0.00001f) continue;
        //
        //             openSet.Enqueue (tri);
        //         }
        //     }
        // }

        Profiler.EndSample ();
        //Debug.Log ($"IN RANGE COUNT: {inRange.Count}. Visited {visitedTris.Count}");
        return inRange;
    }

    private void GetVertexIndexes (int triangleIndex, List<int> verts)
    {
        verts.Add (_meshTriangles[triangleIndex * 3 + 0]);
        verts.Add (_meshTriangles[triangleIndex * 3 + 1]);
        verts.Add (_meshTriangles[triangleIndex * 3 + 2]);
    }

    private List<Triangle> GetNeighbours (Triangle triangle)
    {
        var vertIndices = new List<int> ();
        var triIndexSet = new HashSet<int> ();
        GetVertexIndexes (triangle.Index, vertIndices);

        foreach (var vertIndex in vertIndices)
        {
            foreach (var triIndex in _vertexTriangleLookUp[vertIndex])
            {
                triIndexSet.Add (triIndex);
            }
        }

        var triList = new List<Triangle> ();
        foreach (var triIndex in triIndexSet)
        {
            triList.Add (_raycastTriangles[triIndex]);
        }
        
        return triList;
    }

    public Triangle FindBestTriangleIntersection (Ray localSpaceRay, out Vector3 point)
    {
        return FindFirstHitTriangle (localSpaceRay, _raycastTriangles, out point);
    }

    private Triangle FindFirstHitTriangle (Ray localSpaceRay, IEnumerable<Triangle> triangles, out Vector3 point)
    {
        point = default;

        var closestDistance = float.MaxValue;
        var closestTriangle = default(Triangle);

        foreach (var triangle in triangles)
        {
            var hit = triangle.Intersect (localSpaceRay, out var distance);
            if (hit && distance < closestDistance)
            {
                closestDistance = distance;
                closestTriangle = triangle;
            }
        }

        point = localSpaceRay.origin + localSpaceRay.direction * closestDistance;
        return closestTriangle;
    }
    
    public void Dispose ()
    {
        if (_mesh) Object.Destroy (_mesh);
    }

    public void DrawGizmos ()
    {
        if (!_skinnedMeshRenderer) return;

        var prevColor = Gizmos.color;

        var meshTransform = _skinnedMeshRenderer.transform;
        var bounds = _skinnedMeshRenderer.bounds;

        Gizmos.color = new Color (1, 1, 1, 0.2f);

        Gizmos.DrawWireCube (bounds.center, bounds.size);

        foreach (var vertex in _meshVertices)
        {
            Gizmos.DrawWireSphere (vertex, 0.01f);
        }

        Gizmos.color = Color.blue;

        foreach (var vertex in _lastIntersectingVertices)
        {
            Gizmos.DrawSphere (vertex, 0.01f);
        }

        Gizmos.color = prevColor;
    }

    private Vector3 ClosestPointOnRay (Ray ray, Vector3 point)
    {
        var direction = ray.direction.normalized;
        var lhs = point - ray.origin;

        var dotP = Vector3.Dot (lhs, direction);

        return ray.origin + direction * dotP;
    }


    private void BuildRaycastTriangles (bool updateLookUp)
    {
        if (updateLookUp)
        {
            _vertexTriangleLookUp.Clear ();
            for (var i = 0; i < _meshVertices.Count; i++)
            {
                _vertexTriangleLookUp[i] = new List<int> ();
            }
        }

        var triCount = _meshTriangles.Length / 3;
        for (var i = 0; i < triCount; i++)
        {
            var indexOffset = i * 3;

            var vertexIndex1 = _meshTriangles[indexOffset + 0];
            var vertexIndex2 = _meshTriangles[indexOffset + 1];
            var vertexIndex3 = _meshTriangles[indexOffset + 2];

            var vertex1 = _meshVertices[vertexIndex1];
            var vertex2 = _meshVertices[vertexIndex2];
            var vertex3 = _meshVertices[vertexIndex3];

            _raycastTriangles[i] = new Triangle
            {
                Index = i,
                Vertex1 = vertex1,
                Vertex2 = vertex2,
                Vertex3 = vertex3,
            };

            if (updateLookUp)
            {
                AddToVertexToTriList (vertexIndex1, i);
                AddToVertexToTriList (vertexIndex2, i);
                AddToVertexToTriList (vertexIndex3, i);
            }
        }
    }

    private void AddToVertexToTriList (int vertexIndex1, int i)
    {
        var triList1 = _vertexTriangleLookUp[vertexIndex1];
        if (!triList1.Contains (i)) triList1.Add (i);
    }

    private GraphicsBuffer _buffer;
    public GraphicsBuffer VertexFacingMask ()
    {
        _buffer ??= new GraphicsBuffer (GraphicsBuffer.Target.Index, _mesh.vertexCount, sizeof(float));
        _buffer.SetData (_vertexFacingMask);
        return _buffer;
    }
}