using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;

namespace Depthkit
{
    //data textures are one dataset per triangle, averaged positions, normals and uvs
    [RequireComponent(typeof(Depthkit.CoreMeshSource))]
    [AddComponentMenu("Depthkit/Core/Sources/Depthkit Core Visual Effect Graph Texture Source")]
    public class CoreVFXTextureSource : VFXTextureSource
    {
        #region Properties
        protected static class CoreVFXTextureSourceShaderIds
        {
            public static readonly int
                _VertexBufferDimensions = Shader.PropertyToID("_VertexBufferDimensions");
        }

        [HideInInspector]
        public Vector2 vertexBufferDimensions { get; private set; } = Vector2.zero;

        private Depthkit.CoreMeshSource m_meshSource;

        public override MeshSource meshSource
        {
            get { return m_meshSource; }
        }
        #endregion

        #region TextureSource
        public override string GetComputeShaderName()
        {
            return "Shaders/DataSource/CopyCoreMeshToTextures";
        }

        public override string GetComputeKernelBaseName()
        {
            return "CopyCoreMeshToTextures";
        }

        protected override void SetMeshSource()
        {
            m_meshSource = GetComponent<Depthkit.CoreMeshSource>();
        }

        protected override void SetCommonProperties(ref ComputeShader compute, int kernel)
        {
            compute.SetVector(CoreVFXTextureSourceShaderIds._VertexBufferDimensions, vertexBufferDimensions);
        }

        #endregion

        #region DataSource
        public override string DataSourceName()
        {
            return "Depthkit Core VFX Texture Source";
        }

        protected override bool OnResize()
        {   
            if(m_meshSource == null)
            {
                Debug.Log("Failed to resize: core mesh source is null");
                return false;
            }

            vertexBufferDimensions = m_meshSource.latticeResolution;
            dataTextureSize = m_meshSource.latticeResolution / textureSizeReductionFactor;

            return base.OnResize();
        }
        #endregion

        #region IPropertyTransfer
        public override void SetProperties(ref ComputeShader compute, int kernel)
        {
            compute.SetVector(CoreVFXTextureSourceShaderIds._VertexBufferDimensions, vertexBufferDimensions);
            base.SetProperties(ref compute, kernel);
        }

        public override void SetProperties(ref Material material)
        {
            material.SetVector(CoreVFXTextureSourceShaderIds._VertexBufferDimensions, vertexBufferDimensions);
            base.SetProperties(ref material);
        }

        public override void SetProperties(ref Material material, ref MaterialPropertyBlock block)
        {
            block.SetVector(CoreVFXTextureSourceShaderIds._VertexBufferDimensions, vertexBufferDimensions);
            base.SetProperties(ref material, ref block);
        }
        #endregion
    }
}