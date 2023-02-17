using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEditor.Experimental.TerrainAPI;

namespace Depthkit
{
    [CustomEditor(typeof(MeshDownloader))]
    public class MeshDownloaderEditor : Editor
    {
        SerializedProperty prioritizedPerspective;
        SerializedProperty viewDirection;
        SerializedProperty gammaColor;

        private void OnEnable()
        {
            prioritizedPerspective = serializedObject.FindProperty("prioritizedPerspective");
            viewDirection = serializedObject.FindProperty("viewDirection");
            gammaColor = serializedObject.FindProperty("gammaColor");
        }

        public override void OnInspectorGUI()
        {
            MeshDownloader source = target as MeshDownloader;

            serializedObject.Update();

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Download from GPU"))
            {
                source.Generate();
            }
        }
    }
}