using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Depthkit
{
    public enum ExportUnits
    { 
        Meters,
        Millimeters
    }

    public enum IndexingMode
    {
        ZeroBased,
        OneBased
    }

    [AddComponentMenu("Depthkit/Studio/Mesh Sequence/Depthkit Geometry Sequence Exporter")]
    [RequireComponent(typeof(MeshDownloader))]
    [ExecuteInEditMode]
    public class GeometrySequenceExporter : MonoBehaviour
    {
        public MeshFilter meshFilter = null;
        public PlayableDirector playableDirector = null;
        public StudioMeshSource meshSource = null;

        public ImageFormat textureImageFormat = ImageFormat.PNG;
        public IndexingMode indexingMode = IndexingMode.OneBased;
        public string filenamePrefix = "mesh-f";

        private int m_startFrame = 0;
        private int m_currentFrame;
        private int m_lastRecievedFrame;
        private int m_lastWrittenFrame;

        protected StudioLook m_studioLook = null;
        protected StudioMeshSequenceTextureSource m_textureSource = null;

        private Texture2D m_atlasTexture = null;
        private int m_textureWidth = 0;
        private int m_textureHeight = 0;

        public string outputPath = "Output/PLY/";

        public int maxFrames = 0;

        public bool exportTextures = false;

        private int m_numRunningTasks = 0;
      
        private bool m_bIsRecording = false;

        private MeshDownloader m_meshDownloader = null;
        void StopExport()
        {
            Debug.Log("Capturing Finished - " + (m_currentFrame - m_startFrame) + " frames processed. " + (m_currentFrame - m_lastWrittenFrame -1) + " frame(s) left to be written out.");
            m_bIsRecording = false;
            playableDirector.Stop();
            meshSource.clip.newFrame -= OnNewFrame;
        }

        public bool startExport
        {
            get { return m_bIsRecording; }
            set {  
                if (value && !m_bIsRecording)
                {
                    m_bIsRecording = value;
                    meshSource.clip.newFrame += OnNewFrame;
                    Start();
                }
                if (!value && m_bIsRecording)
                {
                    StopExport();
                }
            }
        }

        [Range(0.0f, 1000.0f)]
        public float scaleFactor = 1000.0f;

        private Task[] m_writerTasks = null;

        // Start is called before the first frame update
        void Start()
        {
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            m_writerTasks = new Task[SystemInfo.processorCount];
            for (int i = 0; i < m_writerTasks.Length; i++)
            {
                m_writerTasks[i] = Task.CompletedTask;
            }
            m_numRunningTasks = 0;

            Initialize();
            meshSource.useTextureAtlas = exportTextures;
            if (playableDirector != null) 
            {
                m_currentFrame = m_startFrame;
                m_lastRecievedFrame = m_lastWrittenFrame = -1;

                playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
                playableDirector.time = playableDirector.duration;
                playableDirector.Evaluate();
            }
        }

        private void Initialize()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = GetComponentInParent<MeshFilter>();
                }
            }

            playableDirector = GetComponent<PlayableDirector>();
            if (playableDirector == null)
            {
                playableDirector = GetComponentInParent<PlayableDirector>();
            }

            if (meshSource == null)
            {
                meshSource = GetComponent<StudioMeshSource>();
                if (meshSource == null)
                {
                    meshSource = GetComponentInParent<StudioMeshSource>();
                }
            }
            if(m_meshDownloader == null)
            { 
                if (meshSource)
                {
                    m_meshDownloader = meshSource.GetChild<MeshDownloader>();
                }
            }
            if (m_textureSource == null)
            { 
                m_textureSource = GetComponent<StudioMeshSequenceTextureSource>();
                if (m_textureSource == null)
                {
                    m_textureSource = GetComponentInParent<StudioMeshSequenceTextureSource>();
                    if (m_textureSource == null)
                    {
                        m_textureSource = gameObject.AddComponent<StudioMeshSequenceTextureSource>();
                    }
                }
            }
            if (m_studioLook == null)
            {
                m_studioLook = GetComponent<StudioLook>();
                if (m_studioLook == null)
                {
                    m_studioLook = GetComponentInParent<StudioLook>();
                }
            }
        }

        private void Awake()
        {
            Initialize();
        }
        private void Reset()
        {
            Initialize();
        }

        void Update()
        {
            if (playableDirector == null)
            {
                return;
            }

            if (m_bIsRecording)
            {
                if (m_lastWrittenFrame >= 0 && m_lastWrittenFrame == m_lastRecievedFrame && m_lastRecievedFrame == m_currentFrame)
                {
                    m_currentFrame++;
                }
                playableDirector.time = m_currentFrame / ((TimelineAsset)playableDirector.playableAsset).editorSettings.fps;
                playableDirector.Evaluate();

                if (m_meshDownloader != null)
                {
                    meshFilter.sharedMesh = m_meshDownloader.mesh;
                }
            }
        }

        void OnNewFrame()
        {
            m_lastRecievedFrame = m_currentFrame;
        }

        void LateUpdate()
        {
            if (m_bIsRecording)
            {

                if (m_lastRecievedFrame != m_lastWrittenFrame && (maxFrames == 0 || (maxFrames > 0 && m_lastRecievedFrame < maxFrames)))
                {
                    StartCoroutine(ProcessRecording(m_lastRecievedFrame));
                    m_lastWrittenFrame = m_lastRecievedFrame;
                }
                if ((playableDirector.duration - playableDirector.time < double.Epsilon) || (maxFrames > 0 && m_currentFrame >= maxFrames)) 
                {
                    StopExport();
                }
            }
        }

        IEnumerator ProcessRecording(int currFrame)
        {
            yield return new WaitForEndOfFrame();

            if (!Directory.Exists(outputPath) || (maxFrames > 0 && currFrame >= maxFrames) || (maxFrames == 0 && !m_bIsRecording)) yield break;
            if (playableDirector != null && playableDirector.duration - playableDirector.time < double.Epsilon) yield break;

            if (meshFilter.sharedMesh.vertexCount == 0 || meshFilter.sharedMesh.triangles.Length == 0)
            {
                Debug.Log("Mesh contains no data at frame "+ currFrame);
                yield break;
            }
            if (meshFilter.sharedMesh.normals.Length != meshFilter.sharedMesh.vertexCount)
            {
                Debug.Log("Per-vertex normals not found.");
                yield break;
            }

            if (exportTextures && meshFilter.sharedMesh.uv.Length != meshFilter.sharedMesh.vertexCount)
            {
                Debug.Log("Per-vertex uvs not found.");
                yield break;
            }

            int taskId = Task.WaitAny(m_writerTasks, 1);

            if(taskId == -1)
            {
                for (taskId = 0; taskId < m_writerTasks.Length; taskId++)
                {
                    if (m_writerTasks[taskId] == null) break;
                }

                while (taskId == m_writerTasks.Length || taskId == -1)
                {
                    taskId = Task.WaitAny(m_writerTasks, 100);
                }
            }

            if (m_writerTasks[taskId] != null)
            {
                if (m_writerTasks[taskId].Status == TaskStatus.Faulted)
                {
                    throw m_writerTasks[taskId].Exception;
                }
                m_writerTasks[taskId].Dispose();
                m_numRunningTasks--;
            }
            if(currFrame != Mathf.RoundToInt((float)(playableDirector.time * ((TimelineAsset)playableDirector.playableAsset).editorSettings.fps)))
                Debug.Log("Playable Director frame is not the same as the exporter current frame");
            string filename = Path.Combine(outputPath, filenamePrefix + (currFrame + (indexingMode == IndexingMode.OneBased ? 1 : 0)).ToString("D5"));
            Vector3[] vertices = meshFilter.sharedMesh.vertices;
            Vector3[] normals = meshFilter.sharedMesh.normals;
            Vector2[] uvs = meshFilter.sharedMesh.uv;
            int[] triangles = meshFilter.sharedMesh.triangles;

            byte[]  textureData = null;
            UnityEngine.Experimental.Rendering.GraphicsFormat textureGraphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;

            if (exportTextures)
            {
                RenderTexture currentRT = RenderTexture.active;
                RenderTexture.active = m_textureSource.rtTextureAtlas;
                textureGraphicsFormat = m_textureSource.rtTextureAtlas.graphicsFormat;

                if (m_atlasTexture == null || m_textureWidth != m_textureSource.rtTextureAtlas.width || m_textureHeight != m_textureSource.rtTextureAtlas.height)
                {
                    m_textureWidth = m_textureSource.rtTextureAtlas.width;
                    m_textureHeight = m_textureSource.rtTextureAtlas.height;
                    m_atlasTexture = new Texture2D(m_textureWidth, m_textureHeight, TextureFormat.RGBA32, 0, true);
                }
                m_atlasTexture.ReadPixels(new Rect(0, 0, m_textureWidth, m_textureHeight), 0, 0);

                RenderTexture.active = currentRT;

                textureData = m_atlasTexture.GetPixelData<byte>(0).ToArray();
            }

            m_writerTasks[taskId] = Task.Run(() =>
            {
                try
                {
                    MeshUtilities.WeldVerts(ref vertices, ref normals, ref uvs, ref triangles);
                    MeshUtilities.ScaleAndMirror(ref vertices, ref normals, ref uvs, ref triangles, scaleFactor, false, false, true);

                    MeshUtilities.WriteMeshPLYFile(filename + ".ply", vertices, normals, exportTextures? uvs: null, triangles, textureImageFormat);
                    if (textureData != null)
                    {
                        switch (textureImageFormat)
                        {
                            case ImageFormat.PNG:
                                File.WriteAllBytes(filename + ".png", ImageConversion.EncodeArrayToPNG(textureData, textureGraphicsFormat, (uint)m_textureWidth, (uint)m_textureHeight));
                                break;
                            case ImageFormat.JPG:
                                File.WriteAllBytes(filename + ".jpg", ImageConversion.EncodeArrayToJPG(textureData, textureGraphicsFormat, (uint)m_textureWidth, (uint)m_textureHeight));
                                break;

                            default:
                                Debug.LogError("Unknown image format");
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    throw e;
                }
            }
            );
            m_numRunningTasks++;
        }
    }
}
