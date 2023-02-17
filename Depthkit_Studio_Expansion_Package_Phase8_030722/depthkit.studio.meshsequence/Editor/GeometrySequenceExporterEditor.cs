using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Depthkit
{
    [CustomEditor(typeof(GeometrySequenceExporter))]
    public class GeometrySequenceExporterEditor : Editor
    {
        public static readonly GUIContent s_outputPathLabel = new GUIContent("Output Path", "The folder into which the exported geometry sequence will be saved.");
        public static readonly GUIContent s_filenamePrefixLabel = new GUIContent("Filename Prefix", "The filename prefix for the frame sequence that will have the frame number and file extension appended to it.");
        public static readonly GUIContent s_frameIndexingModeLabel = new GUIContent("Frame Indexing Mode", "Select whether the output frame sequence starts at zero or one.");
        public static readonly GUIContent s_maxFramesLabel = new GUIContent("Max. Frames to Export", "If this value is set to 0, all the frames in the clip will be exported otherwise the number of exported frames is capped to the number set.");
        public static readonly GUIContent s_exportUnitsIsMetersLabel = new GUIContent("Export Units", "Select the units in which the geometry will be exported.");
        public static readonly GUIContent s_exportImageFormatLabel = new GUIContent("Texture Format", "Select the file format in which the texture will be exported.");
        public static readonly GUIContent s_exportTexturesLabel = new GUIContent("Export Textures", "Check this box to export textured geometry sequences.");
        public static readonly GUIContent s_stopExportButtonLabel = new GUIContent("Stop Export");
        public static readonly GUIContent s_startExportButtonEnabledLabel = new GUIContent("Start Export", "Click to start exporting geometry");
        public static readonly GUIContent s_startExportButtonDisabledLabel = new GUIContent("Start Export", "Exporting is available in Play mode only");
        private static GUIStyle _outputPathButtonStyle = null;
        private static GUIStyle _exportButtonStyle = null;

        //intentionally left blank to hide inherited properties
        private void OnEnable(){
        }

        public override void OnInspectorGUI()
        {
            if (_exportButtonStyle == null)
            {
                _exportButtonStyle = new GUIStyle(GUI.skin.button);
            }
            if (_outputPathButtonStyle == null)
            {
                _outputPathButtonStyle = new GUIStyle(GUI.skin.button);
                _outputPathButtonStyle.alignment = TextAnchor.MiddleLeft;
            }

            GeometrySequenceExporter geomExporter = target as GeometrySequenceExporter;

            GUI.enabled = !geomExporter.startExport;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(s_outputPathLabel, GUILayout.Width(EditorGUIUtility.labelWidth-1));
            bool updatePath = GUILayout.Button(new GUIContent(geomExporter.outputPath, geomExporter.outputPath), _outputPathButtonStyle);
            EditorGUILayout.EndHorizontal();

            if (updatePath)
            {
                string path = EditorUtility.SaveFolderPanel("Output Path", geomExporter.outputPath, "");
                if (!string.IsNullOrEmpty(path) && path != geomExporter.outputPath)
                {
                    geomExporter.outputPath = path;
                    EditorUtility.SetDirty(geomExporter);
                } 
            }

            string filenamePrefix = EditorGUILayout.TextField(s_filenamePrefixLabel, geomExporter.filenamePrefix);
            if (filenamePrefix != geomExporter.filenamePrefix)
            {
                geomExporter.filenamePrefix = filenamePrefix;
                EditorUtility.SetDirty(geomExporter);
            }

            IndexingMode chosenIndexingMode = (IndexingMode)EditorGUILayout.EnumPopup(s_frameIndexingModeLabel, geomExporter.indexingMode);
            if (geomExporter.indexingMode != chosenIndexingMode)
            {
                geomExporter.indexingMode = chosenIndexingMode;
                EditorUtility.SetDirty(geomExporter);
            }

            int maxFrames = EditorGUILayout.IntField(s_maxFramesLabel, geomExporter.maxFrames);
            if( maxFrames != geomExporter.maxFrames)
            {
                geomExporter.maxFrames = maxFrames;
                EditorUtility.SetDirty(geomExporter);
            }

            ExportUnits currentExportScale = (geomExporter.scaleFactor == 1000.0) ? ExportUnits.Millimeters : ExportUnits.Meters;
            ExportUnits exportScale = (ExportUnits)EditorGUILayout.EnumPopup(s_exportUnitsIsMetersLabel, currentExportScale);
            if( exportScale != currentExportScale)
            {
                geomExporter.scaleFactor = (exportScale == ExportUnits.Meters) ? 1.0f : 1000.0f;
                EditorUtility.SetDirty(geomExporter);
            }

            bool exportTextures = EditorGUILayout.Toggle(s_exportTexturesLabel, geomExporter.exportTextures);
            if(exportTextures != geomExporter.exportTextures)
            {
                geomExporter.exportTextures = exportTextures;
                EditorUtility.SetDirty(geomExporter);
            }
            if (exportTextures)
            {
                ImageFormat chosenFormat = (ImageFormat)EditorGUILayout.EnumPopup(s_exportImageFormatLabel, geomExporter.textureImageFormat);
                if (chosenFormat != geomExporter.textureImageFormat)
                {
                    geomExporter.textureImageFormat = chosenFormat;
                    EditorUtility.SetDirty(geomExporter);
                }
            }

            EditorGUILayout.Space();

            GUI.enabled = Application.isPlaying;
            bool pressed = GUILayout.Button((geomExporter.startExport ? s_stopExportButtonLabel : (GUI.enabled)? s_startExportButtonEnabledLabel: s_startExportButtonDisabledLabel), _exportButtonStyle);
            if (pressed)
            {
                geomExporter.startExport = !geomExporter.startExport;
                EditorUtility.SetDirty(geomExporter);
            }
            EditorGUILayout.Space();
        }
    }
}