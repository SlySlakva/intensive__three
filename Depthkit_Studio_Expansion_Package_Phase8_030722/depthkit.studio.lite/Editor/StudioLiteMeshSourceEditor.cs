using UnityEngine;
using UnityEditor;
using System;

namespace Depthkit
{
    [CustomEditor(typeof(Depthkit.StudioLiteMeshSource))]
    public class StudioLiteMeshSourceEditor : Editor
    {
        private bool m_showTextures = false;
        private bool m_showRelighting = false;
        private bool m_showAdvanced = false;

        SerializedProperty meshDensity;
        SerializedProperty adjustableNormalSlope;
        SerializedProperty normalGenerationTechnique;
        SerializedProperty pauseDataGenerationWhenInvisible;
        SerializedProperty pausePlayerWhenInvisible;
        SerializedProperty radialBias;

        MaskGeneratorGUI m_maskGUI;

        void OnEnable()
        {
            meshDensity = serializedObject.FindProperty("m_meshDensity");
            normalGenerationTechnique = serializedObject.FindProperty("normalGenerationTechnique");
            adjustableNormalSlope = serializedObject.FindProperty("adjustableNormalSlope");
            pauseDataGenerationWhenInvisible = serializedObject.FindProperty("pauseDataGenerationWhenInvisible");
            pausePlayerWhenInvisible = serializedObject.FindProperty("pausePlayerWhenInvisible");
            radialBias = serializedObject.FindProperty("radialBias");

            if(m_maskGUI == null)
            {
                m_maskGUI = new MaskGeneratorGUI();
            }
        }

        private void OnDisable()
        {
            m_maskGUI?.Release();
        }

        public override void OnInspectorGUI()
        {
            StudioLiteMeshSource meshSource = target as Depthkit.StudioLiteMeshSource;
            CoreMeshSource coreMeshSource = meshSource;
            bool doGenerate = false;
            bool doResize = false;

            serializedObject.Update();

            if (meshSource.clip == null) 
                return;

            EditorGUI.BeginChangeCheck();

            meshSource.maxPerspectivesToRender = EditorGUILayout.IntSlider("Perspective Limit", meshSource.maxPerspectivesToRender, 1, meshSource.clip.metadata.perspectivesCount);

            EditorGUILayout.PropertyField(normalGenerationTechnique, CoreMeshSourceEditor.s_normalGenTechniqueLabel);
            if (GUI.changed)
            {
                doResize = true;
                doGenerate = true;
                GUI.changed = false;
            }

            NormalGenerationTechnique normalTechnique = (NormalGenerationTechnique)normalGenerationTechnique.enumValueIndex;

            if (normalTechnique == NormalGenerationTechnique.Adjustable ||
                normalTechnique == NormalGenerationTechnique.AdjustableSmoother)
            {
                EditorGUILayout.PropertyField(adjustableNormalSlope, CoreMeshSourceEditor.s_normalSlopeLabel);
                if (GUI.changed)
                {
                    doGenerate = true;
                    GUI.changed = false;
                }
            }

            CoreMeshSourceEditor.MeshSettingsGUI(ref coreMeshSource, ref doResize, ref doGenerate);

            if (EditorGUI.EndChangeCheck())
            {
                doResize = true;
                doGenerate = true;
            }
            EditorGUI.BeginChangeCheck();

            bool adaptiveThreshold = false;

            adaptiveThreshold = meshSource.enableAdaptiveThreshold;
            adaptiveThreshold = EditorGUILayout.Toggle("Enable Adaptive Clip Thresholding", adaptiveThreshold);
            if (adaptiveThreshold != meshSource.enableAdaptiveThreshold)
            {
                meshSource.enableAdaptiveThreshold = adaptiveThreshold;
            }

            if (adaptiveThreshold)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Clipping Threshold");
                float validate;
                validate = EditorGUILayout.FloatField(float.Parse(meshSource.minClipThreshold.ToString("F3")));
                if (validate > 0.001 && validate < meshSource.clipThreshold)
                {
                    meshSource.minClipThreshold = validate;
                }
                EditorGUILayout.MinMaxSlider(ref meshSource.minClipThreshold, ref meshSource.clipThreshold, 0.001f, 1.0f);
                validate = EditorGUILayout.FloatField(float.Parse(meshSource.clipThreshold.ToString("F3")));
                if (validate > meshSource.minClipThreshold && validate <= 1.0)
                {
                    meshSource.clipThreshold = validate;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Dithering Width");
                validate = EditorGUILayout.FloatField(float.Parse(meshSource.minDitherWidth.ToString("F3")));
                if (validate > 0.0 && validate < meshSource.ditherWidth)
                {
                    meshSource.minDitherWidth = validate;
                }
                EditorGUILayout.MinMaxSlider(ref meshSource.minDitherWidth, ref meshSource.ditherWidth, 0.0f, 0.2f);
                validate = EditorGUILayout.FloatField(float.Parse(meshSource.ditherWidth.ToString("F3")));
                if (validate > meshSource.minDitherWidth && validate <= 0.2)
                {
                    meshSource.ditherWidth = validate;
                }
                EditorGUILayout.EndHorizontal();

                float angleInDegrees = Mathf.Acos(meshSource.maxViewAngleCosThreshold) * 180.0f / Mathf.PI;
                angleInDegrees = EditorGUILayout.Slider("Max View Angle Threshold", angleInDegrees, 0.0f, 90.0f);
                meshSource.maxViewAngleCosThreshold = Mathf.Cos(angleInDegrees * Mathf.PI / 180.0f);
            }
            else
            {
                meshSource.clipThreshold = EditorGUILayout.Slider("Clipping Threshold", meshSource.clipThreshold, 0.0f, 1.0f);
                meshSource.ditherWidth = EditorGUILayout.Slider("Dithering Width", meshSource.ditherWidth, 0.0f, 0.2f);
            }
            if (EditorGUI.EndChangeCheck())
            {
                doGenerate = true;
            }

            EditorGUI.BeginChangeCheck();

            m_maskGUI.MaskGui(ref meshSource.maskGenerator, meshSource.meshDensity, ref doGenerate);

            EditorGUI.BeginChangeCheck();


            m_showAdvanced = EditorGUILayout.Foldout(m_showAdvanced, "Advanced Settings");
            if (m_showAdvanced)
            {
                EditorGUILayout.PropertyField(radialBias, new GUIContent("Depth Bias Compensation", "Time of Flight cameras measure surfaces farther away than they are in reality. The amount of bias depends greatly on the material of the surface being measured. Skin in particular has a large bias. The Depth Bias Compensation is a correction for this error by pulling the surface back towards their true depth. It most useful for recovering high quality faces and hands on otherwise well-calibrated captures. The larger the value, the larger the compensation. 0 means no depth bias compensation is applied."));
                meshSource.edgeCompressionNoiseThreshold = EditorGUILayout.Slider("Edge Compression Noise Threshold", meshSource.edgeCompressionNoiseThreshold, 0.0f, 1.0f);
                EditorGUILayout.PropertyField(pausePlayerWhenInvisible);
                EditorGUILayout.PropertyField(pauseDataGenerationWhenInvisible);
            }

            if (EditorGUI.EndChangeCheck())
            {
                doResize = true;
                doGenerate = true;
            }

            if (meshSource.transform.hasChanged)
            {
                doGenerate = true;
                meshSource.transform.hasChanged = false;
            }

            serializedObject.ApplyModifiedProperties();

            if(doResize)
            {
                meshSource.Resize();
            }
            if(doGenerate)
            {
                m_maskGUI.MarkDirty();
                meshSource.Generate();
            }
        }
    }
}