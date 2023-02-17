using UnityEngine;
using System.Collections;

namespace Depthkit
{
    [AddComponentMenu("Depthkit/Studio/Mesh Sequence/Depthkit Studio Mesh Source Downloader Preview")]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class MeshDownloaderPreview : MonoBehaviour
    {
        public Depthkit.Clip depthkitClip;

        [SerializeField, HideInInspector]
        MeshDownloader m_meshDownloader = null;
        public MeshDownloader meshDownloader { 
            get {
                FindClip();
                FindDownloader();
                return m_meshDownloader;
            } 
        }

        Material m_material = null;
        MeshFilter m_filter = null;
        MeshRenderer m_meshRenderer = null;

        public bool HasDownloadedMesh()
        {
            return m_meshDownloader != null && m_meshDownloader.HasDownloadedMesh();
        }

        void FindClip()
        {
            depthkitClip = GetComponentInParent<Depthkit.Clip>();
            //search up the hierarchy
            if (depthkitClip == null && transform.parent != null)
            {
                var p = transform.parent;
                while (p != null && depthkitClip == null)
                {
                    depthkitClip = p.GetComponent<Depthkit.Clip>();
                    p = p.parent;
                }
            }
        }

        void FindDownloader()
        {
            if (depthkitClip != null && m_meshDownloader == null)
            {
                StudioMeshSource meshSource = depthkitClip.GetDataSource<StudioMeshSource>();
                if(meshSource)
                {
                    m_meshDownloader = meshSource.GetChild<MeshDownloader>();
                }
            }
        }

        private void Awake()
        {
            FindClip();
            FindDownloader();
            m_filter = GetComponent<MeshFilter>();
            m_meshRenderer = GetComponent<MeshRenderer>();
        }

        void Start()
        {
            m_material = new Material(Shader.Find("Depthkit/Studio/MeshDownloaderPreview"));
            m_meshRenderer.sharedMaterial = m_material;
            FindClip();
        }

        void Update()
        {
            FindDownloader();

            if (m_meshDownloader != null)
            {
                m_filter.sharedMesh = m_meshDownloader.mesh;
            }
        }
    }
}