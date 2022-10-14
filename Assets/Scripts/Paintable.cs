using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class Paintable : MonoBehaviour {
    const int TEXTURE_SIZE = 1024;

    public float extendsIslandOffset = 1;

    RenderTexture extendIslandsRenderTexture;
    RenderTexture uvIslandsRenderTexture;
    RenderTexture maskRenderTexture;
    RenderTexture supportTexture;
    
    public Renderer rend;
    public SkinnedMeshRenderer _skinnedMeshRenderer;
    public MeshCollider _MeshCollider;

    int maskTextureID = Shader.PropertyToID("_MaskTexture");

    public RenderTexture getMask() => maskRenderTexture;
    public RenderTexture getUVIslands() => uvIslandsRenderTexture;
    public RenderTexture getExtend() => extendIslandsRenderTexture;
    public RenderTexture getSupport() => supportTexture;
    public Renderer getRenderer() => _skinnedMeshRenderer ? _skinnedMeshRenderer : rend;
    public Mesh GetMesh () => _mesh ? _mesh : rend.GetComponent<MeshFilter> ().sharedMesh;

    public Matrix4x4 GetLocalToWorld ()
    {
        return transform.localToWorldMatrix;
    }

    private Mesh _mesh;
    private SkinnedMeshRaycaster _skinnedMeshRaycaster;
    
    public bool IsSkinnedMesh => _skinnedMeshRenderer;

    private void Update ()
    {
        if (_skinnedMeshRenderer && _MeshCollider)
        {
            _skinnedMeshRenderer.BakeMesh (_mesh, true);
            _MeshCollider.sharedMesh = _mesh;
        }
    }

    void Start()
    {
        maskRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        maskRenderTexture.filterMode = FilterMode.Bilinear;

        extendIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        extendIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        uvIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        uvIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        supportTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        supportTexture.filterMode =  FilterMode.Bilinear;

        rend = GetComponent<Renderer>();
        _skinnedMeshRenderer = rend as SkinnedMeshRenderer;

        if (_skinnedMeshRenderer)
        {
            _mesh = Object.Instantiate (_skinnedMeshRenderer.sharedMesh);
        }
        
        rend.material.SetTexture(maskTextureID, extendIslandsRenderTexture);

        PaintManager.instance.initTextures(this);
    }

    void OnDisable(){
        maskRenderTexture.Release();
        uvIslandsRenderTexture.Release();
        extendIslandsRenderTexture.Release();
        supportTexture.Release();
    }


    public bool TryGetPointOnMesh (Ray ray, Vector3 originalHitPoint, out Vector3 point)
    {
        if (_skinnedMeshRenderer && _skinnedMeshRaycaster != default)
        {
           _skinnedMeshRaycaster.Raycast (ray, 0.1f, out point);
        }

        point = originalHitPoint;
        return true;
    }
}