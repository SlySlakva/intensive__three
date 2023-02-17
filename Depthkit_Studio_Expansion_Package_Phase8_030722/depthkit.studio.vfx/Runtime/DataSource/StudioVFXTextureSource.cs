using UnityEngine;
using System.Runtime.InteropServices;

namespace Depthkit
{
    //data textures are one dataset per triangle (based on the 0th (non-shared) vertex in the triangle)
    [RequireComponent(typeof(Depthkit.StudioMeshSource))]
    [AddComponentMenu("Depthkit/Studio/Sources/Depthkit Studio Visual Effect Graph Texture Source")]
    public class StudioVFXTextureSource : VFXTextureSource
    {
        #region Properties
        protected static class StudioVFXTextureSourceShaderIds
        {
            public static readonly int
                _ObjectSpaceCameraPosition = Shader.PropertyToID("_ObjectSpaceCameraPosition"),
                _DataTextureSize = Shader.PropertyToID("_DataTextureSize");
        }

        private StudioMeshSource m_meshSource;

        public override MeshSource meshSource
        {
            get { return m_meshSource; }
        }
        #endregion

        #region TextureSource
        public override string GetComputeShaderName()
        {
            return "Shaders/DataSource/CopyStudioMeshToTextures";
        }

        public override string GetComputeKernelBaseName()
        {
            return "CopyStudioMeshToTextures";
        }

        protected override void SetMeshSource()
        {
            m_meshSource = GetComponent<Depthkit.StudioMeshSource>();
        }

        protected override void SetCommonProperties(ref ComputeShader compute, int kernel)
        {
            Vector3 objectCamera = transform.worldToLocalMatrix * new Vector4(Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z, 1);
            compute.SetVector(StudioVFXTextureSourceShaderIds._ObjectSpaceCameraPosition, objectCamera);
            compute.SetVector(StudioVFXTextureSourceShaderIds._DataTextureSize, new Vector2(dataTextureSize.x, dataTextureSize.y));
        }

        #endregion

        #region DataSource
        public override string DataSourceName()
        {
            return "Depthkit Studio VFX Texture Source";
        }

        protected override bool OnResize()
        {
            if (m_meshSource == null)
            {
                Debug.Log("Failed to resize: studio mesh source is null");
                return false;
            }

            int maxTriangleCount = (int)Mathf.Max(Mathf.Ceil(Mathf.Sqrt(m_meshSource.maxSurfaceTriangles / textureSizeReductionFactor)), 16);

            dataTextureSize = new Vector2Int(maxTriangleCount, maxTriangleCount);

            return base.OnResize();

        }
        #endregion

        #region IPropertyTransfer
        public override void SetProperties(ref ComputeShader compute, int kernel)
        {
            compute.SetVector(StudioVFXTextureSourceShaderIds._DataTextureSize, new Vector2(dataTextureSize.x, dataTextureSize.y));
            base.SetProperties(ref compute, kernel);
        }

        public override void SetProperties(ref Material material)
        {
            material.SetVector(StudioVFXTextureSourceShaderIds._DataTextureSize, new Vector2(dataTextureSize.x, dataTextureSize.y));
            base.SetProperties(ref material);
        }

        public override void SetProperties(ref Material material, ref MaterialPropertyBlock block)
        {
            block.SetVector(StudioVFXTextureSourceShaderIds._DataTextureSize, new Vector2(dataTextureSize.x, dataTextureSize.y));
            base.SetProperties(ref material, ref block);
        }

        #endregion
    }
}