Shader "Custom/CrossHatchInk"
{
    Properties
    {
        _PaperColor ("Paper Color", Color) = (0.98, 0.93, 0.87, 1)
        _InkColor ("Ink Color", Color) = (0.08, 0.06, 0.05, 1)

        // Five hatch textures, from lightest (almost blank) to darkest (dense crosshatch)
        _Hatch0 ("Hatch 0 (brightest)", 2D) = "white" {}
        _Hatch1 ("Hatch 1", 2D) = "white" {}
        _Hatch2 ("Hatch 2", 2D) = "white" {}
        _Hatch3 ("Hatch 3", 2D) = "white" {}
        _Hatch4 ("Hatch 4 (darkest)", 2D) = "white" {}

        _HatchScale ("Hatch Tiling", Float) = 4
        _HatchScreenSpace ("Screen-Space Hatch (0=UV,1=Triplanar-ish World)", Range(0,1)) = 0

        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.01
        _CrackTex ("Crack / Detail Overlay", 2D) = "black" {}
        _CrackStrength ("Crack Strength", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        // ---------- PASS 1: OUTLINE (inverted hull) ----------
        Pass
        {
            Name "Outline"
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
                // Push vertices out along their normals in object space, then transform
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

        // ---------- PASS 2: HATCHED SURFACE ----------
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

            TEXTURE2D(_Hatch0); SAMPLER(sampler_Hatch0);
            TEXTURE2D(_Hatch1); SAMPLER(sampler_Hatch1);
            TEXTURE2D(_Hatch2); SAMPLER(sampler_Hatch2);
            TEXTURE2D(_Hatch3); SAMPLER(sampler_Hatch3);
            TEXTURE2D(_Hatch4); SAMPLER(sampler_Hatch4);
            TEXTURE2D(_CrackTex); SAMPLER(sampler_CrackTex);

            float4 _PaperColor;
            float4 _InkColor;
            float _HatchScale;
            float _HatchScreenSpace;
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
                float4 screenPos   : TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.positionHCS);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 N = normalize(i.normalWS);
                float3 L = normalize(mainLight.direction);

                // Lambert term, remapped so shadows read as fully dark
                float NdotL = saturate(dot(N, L)) * mainLight.shadowAttenuation;

                // Choose UV set: object UVs, or screen-space (keeps hatch lines
                // camera-aligned like pen sketches instead of stretching with surfaces)
                float2 hatchUV = lerp(i.uv, i.screenPos.xy / i.screenPos.w * _ScreenParams.xy / 100.0, _HatchScreenSpace);
                hatchUV *= _HatchScale;

                half h0 = SAMPLE_TEXTURE2D(_Hatch0, sampler_Hatch0, hatchUV).r;
                half h1 = SAMPLE_TEXTURE2D(_Hatch1, sampler_Hatch1, hatchUV).r;
                half h2 = SAMPLE_TEXTURE2D(_Hatch2, sampler_Hatch2, hatchUV).r;
                half h3 = SAMPLE_TEXTURE2D(_Hatch3, sampler_Hatch3, hatchUV).r;
                half h4 = SAMPLE_TEXTURE2D(_Hatch4, sampler_Hatch4, hatchUV).r;

                // Blend between the 5 hatch levels based on light amount.
                // Bright areas -> h0 (near blank), dark areas -> h4 (dense crosshatch)
                half tone;
                float t = NdotL * 4.0; // 0..4 across 5 bands
                if (t < 1.0)      tone = lerp(h1, h0, t);
                else if (t < 2.0) tone = lerp(h2, h1, t - 1.0);
                else if (t < 3.0) tone = lerp(h3, h2, t - 2.0);
                else              tone = lerp(h4, h3, t - 3.0);

                // Overlay hand-drawn crack/detail texture (independent of lighting)
                half crack = SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, i.uv).r;
                tone = saturate(tone - crack * _CrackStrength);

                half4 col = lerp(_InkColor, _PaperColor, tone);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
