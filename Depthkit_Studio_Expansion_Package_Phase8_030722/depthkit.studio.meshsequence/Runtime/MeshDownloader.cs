using UnityEngine;
using System.IO;
using System;

namespace Depthkit
{
    [RequireComponent(typeof(StudioMeshSource))]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu("Depthkit/Studio/Sources/Depthkit Studio Mesh Source Downloader")]
    public class MeshDownloader : DataSource
    {
        [NonSerialized]
        public Mesh mesh;

        public int prioritizedPerspective = -1;
        public Transform viewDirection;
        public bool gammaColor = false;
        public bool refreshDownload = false;

        [SerializeField, HideInInspector]
        StudioMeshSource meshSource;

        public bool HasDownloadedMesh()
        {
            return mesh != null && mesh.vertexCount != 0;
        }

        public override string DataSourceName()
        {
            return "Depthkit Studio Mesh Downloader";
        }

        protected override void OnAwake()
        {
            meshSource = GetComponent<StudioMeshSource>();
        }

        public override bool OnSetup()
        {
            meshSource.GetChild<MeshDownloader>(); //add self to mesh Source

            return true;
        }

        protected override bool OnResize()
        {
            return true;
        }

        protected override bool OnGenerate()
        {
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            if (meshSource == null)
            {
                Debug.LogError("Cannot download, there is no mesh source");
                return false;
            }

            int[] bufferDataTris = new int[1];
            meshSource.CurrentSubMesh().trianglesCount.GetData(bufferDataTris);

            int triangleCount = bufferDataTris[0];
            Depthkit.Core.PackedTriangle[] triangles = new Depthkit.Core.PackedTriangle[triangleCount];
            meshSource.triangleBuffer.GetData(triangles);

            int vertexCount = triangleCount * 3;

            //nothing to do
            if (vertexCount == 0) return false;

            Vector3[] positions = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] tris = new int[vertexCount];

            int i = 0;
            foreach (var tri in triangles)
            {
                positions[i * 3 + 0] = tri.vertex0.position;
                positions[i * 3 + 1] = tri.vertex1.position;
                positions[i * 3 + 2] = tri.vertex2.position;
                normals[i * 3 + 0] = tri.vertex0.normal;
                normals[i * 3 + 1] = tri.vertex1.normal;
                normals[i * 3 + 2] = tri.vertex2.normal;
                uvs[i * 3 + 0] = tri.vertex0.uv;
                uvs[i * 3 + 1] = tri.vertex1.uv;
                uvs[i * 3 + 2] = tri.vertex2.uv;
                tris[i * 3 + 0] = i * 3 + 0;
                tris[i * 3 + 1] = i * 3 + 1;
                tris[i * 3 + 2] = i * 3 + 2;
                ++i;
            }

            mesh.Clear();

            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.uv = uvs;

            return true;
        }

        protected override void OnUpdate()
        {
            if(refreshDownload)
            {
                ScheduleGenerate();
                refreshDownload = false;
            }
            base.OnUpdate();
        }
    }
}