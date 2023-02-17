using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Depthkit
{
    [CustomEditor(typeof(Depthkit.CoreVFXTextureSource))]
    public class CoreVFXTextureSourceEditor : Editor
    {
        SerializedProperty m_bIsSetup;
        SerializedProperty textureSizeReductionFactor;
        SerializedProperty usePowerOf2Size;
        bool m_showTextures = false;

        float m_scale = 0.5f;

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        void OnEnable()
        {
            m_bIsSetup = serializedObject.FindProperty("m_bIsSetup");
            textureSizeReductionFactor = serializedObject.FindProperty("textureSizeReductionFactor");
            usePowerOf2Size = serializedObject.FindProperty("usePowerOf2Size");

            m_bIsSetup.boolValue = false;
        }
        public override void OnInspectorGUI()
        {
            Depthkit.CoreVFXTextureSource texSource = (Depthkit.CoreVFXTextureSource)target;
            serializedObject.Update();
            bool doRefresh = false;
            bool doUpdate = false;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(textureSizeReductionFactor);
            EditorGUILayout.PropertyField(usePowerOf2Size);
            if (EditorGUI.EndChangeCheck())
            {
                doRefresh = true;
            }

            m_showTextures = EditorGUILayout.Foldout(m_showTextures, "Data Textures Preview");
            if(m_showTextures)
            {
                m_scale = EditorGUILayout.Slider("Preview Scale", m_scale, 0.0001f, 1.0f);
                if (texSource.positionDataTexture != null){
                    GUILayout.Label("Position Data:");
                    GUILayout.Label(texSource.positionDataTexture, GUILayout.Height(texSource.positionDataTexture.height * m_scale));
                }
                if(texSource.colorDataTexture != null){
                    GUILayout.Label("Color Data:");
                    GUILayout.Label(texSource.colorDataTexture, GUILayout.Height(texSource.colorDataTexture.height * m_scale));
                }
                if(texSource.normalDataTexture != null){
                    GUILayout.Label("Normal Data:");
                    GUILayout.Label(texSource.normalDataTexture, GUILayout.Height(texSource.normalDataTexture.height * m_scale));
                }
            }

            serializedObject.ApplyModifiedProperties();

            if(GUILayout.Button("Resize"))
            {
                doRefresh = true;
            }

            if(GUILayout.Button("Generate Data"))
            {
                doUpdate = true;
            }

            if(doRefresh)
            {
                texSource.Resize();
            }
            if(doUpdate)
            {
                texSource.Generate();
            }
        }
    }
}