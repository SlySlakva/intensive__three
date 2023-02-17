/************************************************************************************

Depthkit Unity SDK License v1
Copyright 2016-2020 Scatter All Rights reserved.  

Licensed under the Scatter Software Development Kit License Agreement (the "License"); 
you may not use this SDK except in compliance with the License, 
which is provided at the time of installation or download, 
or which otherwise accompanies this software in either electronic or hard copy form.  

You may obtain a copy of the License at http://www.depthkit.tv/license-agreement-v1

Unless required by applicable law or agreed to in writing, 
the SDK distributed under the License is distributed on an "AS IS" BASIS, 
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
See the License for the specific language governing permissions and limitations under the License. 

************************************************************************************/

using UnityEngine;
using UnityEngine.Rendering;

namespace Depthkit
{
    [SelectionBase]
    [ExecuteInEditMode]
    [AddComponentMenu("Depthkit/Studio/Mesh Sequence/Depthkit Studio Mesh Sequence Texture Source")]
    public class StudioMeshSequenceTextureSource : Look
    {
        public RenderTexture rtTextureAtlas = null;
        public Material atlasMaterial = null;

        protected static Shader s_textureAtlasUnlitPhotoLookShader = null;
        protected static Material s_textureAtlasUnlitPhotoLookMaterial = null;

        protected override bool UsesMaterial() { return true; }
        protected override Material GetMaterial() { return atlasMaterial; }

        protected static Shader s_pushPullShader = null;
        protected static Material s_pushPullMaterial = null;

        protected Material pushPullMaterial
        {
            get
            {
                if (s_pushPullShader == null)
                {
                    s_pushPullShader = Shader.Find("Depthkit/Util/PushPullMips");
                }

                if (s_pushPullMaterial == null)
                {
                    s_pushPullMaterial = new Material(s_pushPullShader);
                }
                return s_pushPullMaterial;
            }
        }

        protected static Material GetDefaultMaterial()
        {
            if (s_textureAtlasUnlitPhotoLookShader == null)
            {
                s_textureAtlasUnlitPhotoLookShader = Shader.Find("Depthkit/Studio/Depthkit Studio Photo Look Built-in RP");
            }

            if (s_textureAtlasUnlitPhotoLookMaterial == null)
            {
                s_textureAtlasUnlitPhotoLookMaterial = new Material(s_textureAtlasUnlitPhotoLookShader);
            }
            return s_textureAtlasUnlitPhotoLookMaterial;
        }

        public override string GetLookName() { return "Depthkit Studio Mesh Sequence Texture Source"; }

        protected override void SetDataSources()
        {
            if (meshSource == null)
            {
                meshSource = depthkitClip.GetDataSource<StudioMeshSource>();
            }
        }

        protected override void SetDefaults()
        {
            if (atlasMaterial == null)
            {
                atlasMaterial = GetDefaultMaterial();
            }
            base.SetDefaults();
        }

        protected override void SetLookProperties()
        {
            base.SetLookProperties();

            Util.EnsureKeyword(ref atlasMaterial, "DK_TEXTURE_ATLAS", true);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (meshSource.triangleBufferDrawIndirectArgs == null ||
                !meshSource.triangleBufferDrawIndirectArgs.IsValid() ||
                meshSource.triangleBuffer == null ||
                !meshSource.triangleBuffer.IsValid())
            {
                return;
            }

            if (rtTextureAtlas == null)
            {
                if (rtTextureAtlas != null)
                {
                    rtTextureAtlas.Release();
                }

                float perspectiveAspectRatio = (float)depthkitClip.metadata.perspectiveResolution.x / (float)depthkitClip.metadata.perspectiveResolution.y;
                // if perspective aspect ratio > 1 (width > height), layout vertically, else horizontally
                int width = Mathf.NextPowerOfTwo(depthkitClip.metadata.perspectiveResolution.x * (perspectiveAspectRatio > 1 ? 1 : depthkitClip.metadata.perspectivesCount));
                int height = Mathf.NextPowerOfTwo(depthkitClip.metadata.perspectiveResolution.y * (perspectiveAspectRatio > 1 ? depthkitClip.metadata.perspectivesCount : 1));

                rtTextureAtlas = new RenderTexture(width, height, 1, RenderTextureFormat.ARGB32);
                rtTextureAtlas.enableRandomWrite = true;
                rtTextureAtlas.Create();
            }

            SetLookProperties();

            int mipCount = (int)Mathf.Log(Mathf.Min(rtTextureAtlas.width, rtTextureAtlas.height), 2f) + 1;

            RenderTextureDescriptor tempRTDesc = rtTextureAtlas.descriptor;
            tempRTDesc.useMipMap = true;
            tempRTDesc.mipCount = mipCount;
            tempRTDesc.sRGB = false; // Avoids an issue where generated mips are too dark

            RenderTexture tempRT = RenderTexture.GetTemporary(tempRTDesc);

            CommandBuffer commandBuffer = new CommandBuffer();
            commandBuffer.name = "Render Texture Atlas";
            commandBuffer.SetRenderTarget(new RenderTargetIdentifier(tempRT));
            commandBuffer.ClearRenderTarget(true, true, Color.clear, 1.0f);

            // draw call for the texture atlas
            commandBuffer.DrawProceduralIndirect(
                Matrix4x4.identity,
                GetMaterial(),
                -1,
                MeshTopology.Triangles,
                meshSource.triangleBufferDrawIndirectArgs, 0,
                GetMaterialPropertyBlock());

            Graphics.ExecuteCommandBuffer(commandBuffer);

            // fill using mip map push-pull
            commandBuffer = new CommandBuffer();
            commandBuffer.SetRenderTarget(new RenderTargetIdentifier(rtTextureAtlas));
            commandBuffer.ClearRenderTarget(true, true, Color.clear, 1.0f);

            Texture tempRTTexture = tempRT;
            tempRTTexture.filterMode = FilterMode.Trilinear;

            pushPullMaterial.SetFloat("_MipLevels", mipCount);
            commandBuffer.Blit(tempRTTexture, rtTextureAtlas, pushPullMaterial);
            Graphics.ExecuteCommandBuffer(commandBuffer);

            RenderTexture.ReleaseTemporary(tempRT);
        }
    }
}