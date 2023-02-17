// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Depthkit/Studio/Depthkit Studio Lite Main Edge Stencil Built-in RP"
{
    Properties
    {
        [Toggle(DK_USE_EDGEMASK)] _UseEdgeMask("Use Edge Mask", Float) = 0
    }

    SubShader
    {
        Tags {"RenderType" = "Opaque" "Queue" = "AlphaTest"}
        Cull Back
        LOD 100

        Pass
        {
            Name "Main Edge Stencil"
            Tags {"LightMode" = "Always"}
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            ColorMask 0
            ZWrite Off

            CGPROGRAM

            #pragma shader_feature_local __ DK_USE_EDGEMASK

            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #define DK_USE_BUILT_IN_COLOR_CONVERSION
            #include "Packages/nyc.scatter.depthkit.core/Runtime/Resources/Shaders/Includes/Depthkit.cginc"
            #include "Packages/nyc.scatter.depthkit.core/Runtime/Resources/Shaders/Includes/SampleCoreTriangles.cginc"
            #define DK_SCREEN_DOOR_TRANSPARENCY
            #include "Packages/nyc.scatter.depthkit.core/Runtime/Resources/Shaders/Includes/SampleEdgeMask.cginc"
            #include "Packages/nyc.scatter.depthkit.studio.lite/Runtime/Resources/Shaders/Looks/DepthkitStudioLiteGeomOnly.cginc"
            ENDCG
        }
    }
}
