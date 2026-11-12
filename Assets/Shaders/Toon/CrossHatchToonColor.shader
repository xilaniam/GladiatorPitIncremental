Shader "Custom/CrossHatchToonColor"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color Tint", Color) = (0.9, 0.35, 0.2, 1)
        _ShadowColor ("Shadow Multiply Tint", Color) = (0.5, 0.45, 0.55, 1)
        _InkColor ("Ink Line Color", Color) = (0.05, 0.02, 0.02, 1)
        _HighlightColor ("Specular Highlight Color", Color) = (1,1,1,1)

        _ShadowThreshold ("Shadow Band Threshold", Range(0,1)) = 0.45
        _ShadowSoftness ("Shadow Edge Softness", Range(0.001,0.3)) = 0.05

        _HatchTex ("Shadow Hatch Texture", 2D) = "white" {}
        _HatchScale ("Hatch Tiling", Float) = 6
        _HatchStrength ("Hatch Darkening Strength", Range(0,1)) = 0.8

        _HighlightSize ("Highlight Size", Range(0.9, 0.999)) = 0.985
        _HighlightSoftness ("Highlight Softness", Range(0.001, 0.05)) = 0.01

        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.01
        _CrackTex ("Crack / Detail Overlay", 2D) = "black" {}
        _CrackStrength ("Crack Strength", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        // ---------- PASS 1: OUTLINE (inverted hull, same as CrossHatchInk) ----------
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="Outline" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _OutlineWidth;
            float4 _InkColor;

            struct appdata { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct v2f { float4 positionHCS : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                float3 offsetPos = v.positionOS.xyz + normalize(v.normalOS) * _OutlineWidth;
                o.positionHCS = TransformObjectToHClip(offsetPos);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return _InkColor;
            }
            ENDHLSL
        }

        // ---------- PASS 2: FLAT-COLOR TOON + SHADOW-ONLY HATCH ----------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_HatchTex); SAMPLER(sampler_HatchTex);
            TEXTURE2D(_CrackTex); SAMPLER(sampler_CrackTex);

            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _ShadowColor;
            float4 _InkColor;
            float4 _HighlightColor;
            float _ShadowThreshold;
            float _ShadowSoftness;
            float _HatchScale;
            float _HatchStrength;
            float _HighlightSize;
            float _HighlightSoftness;
            float _CrackStrength;

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 N = normalize(i.normalWS);
                float3 L = normalize(mainLight.direction);
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 H = normalize(L + V);

                float NdotL = saturate(dot(N, L)) * mainLight.shadowAttenuation;

                // Two-band toon shading: smoothstep gives a soft-but-flat edge
                // between the lit color and the shadow tint, instead of a
                // smooth Lambert gradient.
                float litAmount = smoothstep(_ShadowThreshold - _ShadowSoftness,
                                              _ShadowThreshold + _ShadowSoftness,
                                              NdotL);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float3 baseColor = baseSample.rgb * _BaseColor.rgb;
                float3 shadowColor = baseColor * _ShadowColor.rgb;

                float3 diffuse = lerp(shadowColor, baseColor, litAmount);

                // Crosshatch only shows up inside the shadow band - multiply
                // the ink lines into the shadow color, leave the lit side flat.
                float2 hatchUV = i.uv * _HatchScale;
                half hatchSample = SAMPLE_TEXTURE2D(_HatchTex, sampler_HatchTex, hatchUV).r;
                float shadowAmount = 1.0 - litAmount;
                float lineDarken = (1.0 - hatchSample) * shadowAmount * _HatchStrength;
                diffuse = lerp(diffuse, _InkColor.rgb, lineDarken);

                // Hand-drawn crack/detail lines, independent of lighting
                half crack = SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, i.uv).r;
                diffuse = lerp(diffuse, _InkColor.rgb, crack * _CrackStrength);

                // Stylized single specular blob (the little white highlight
                // ellipse on the sphere/cube in the reference)
                float NdotH = saturate(dot(N, H));
                float highlight = smoothstep(_HighlightSize - _HighlightSoftness,
                                              _HighlightSize + _HighlightSoftness,
                                              NdotH);
                float3 finalColor = lerp(diffuse, _HighlightColor.rgb, highlight);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
