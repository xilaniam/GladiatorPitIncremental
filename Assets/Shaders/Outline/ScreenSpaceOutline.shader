Shader "Hidden/ScreenSpaceOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.05, 0.04, 0.03, 1)
        _OutlineThickness ("Outline Thickness (px)", Range(1, 6)) = 2
        _DepthSensitivity ("Depth Sensitivity", Range(0, 5)) = 1
        _NormalSensitivity ("Normal Sensitivity", Range(0, 5)) = 1
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.05

        _RoughnessTex ("Roughness / Break-up Noise", 2D) = "white" {}
        _RoughnessAmount ("Roughness Amount", Range(0, 1)) = 0.4
        _RoughnessScale ("Roughness Tiling", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ScreenSpaceOutline"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BlitTexture); SAMPLER(sampler_BlitTexture);
            TEXTURE2D(_RoughnessTex); SAMPLER(sampler_RoughnessTex);

            float4 _OutlineColor;
            float _OutlineThickness;
            float _DepthSensitivity;
            float _NormalSensitivity;
            float _EdgeSoftness;
            float _RoughnessAmount;
            float _RoughnessScale;

            struct appdata { uint vertexID : SV_VertexID; };
            struct v2f { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                // Full-screen triangle, no vertex/index buffer needed
                float2 uv = float2((v.vertexID << 1) & 2, v.vertexID & 2);
                o.positionHCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                o.uv = uv;
                #if UNITY_UV_STARTS_AT_TOP
                o.uv.y = 1.0 - o.uv.y;
                #endif
                return o;
            }

            float SampleLinearDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            // Reconstructs a rough view-space-ish normal from the depth buffer
            // by comparing depth-derived positions to their screen neighbors.
            float3 ReconstructNormal(float2 uv, float2 texelSize)
            {
                float depthC = SampleLinearDepth(uv);
                float depthX = SampleLinearDepth(uv + float2(texelSize.x, 0));
                float depthY = SampleLinearDepth(uv + float2(0, texelSize.y));

                float3 pC = float3(uv, depthC);
                float3 pX = float3(uv + float2(texelSize.x, 0), depthX);
                float3 pY = float3(uv + float2(0, texelSize.y), depthY);

                return normalize(cross(pY - pC, pX - pC));
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv);

                float2 texelSize = _OutlineThickness / _ScreenParams.xy;

                // ---- Depth-based Sobel: catches silhouettes and intersections
                // between overlapping meshes (like overlapping foliage blobs) ----
                float d_tl = SampleLinearDepth(i.uv + texelSize * float2(-1,  1));
                float d_tr = SampleLinearDepth(i.uv + texelSize * float2( 1,  1));
                float d_bl = SampleLinearDepth(i.uv + texelSize * float2(-1, -1));
                float d_br = SampleLinearDepth(i.uv + texelSize * float2( 1, -1));
                float d_l  = SampleLinearDepth(i.uv + texelSize * float2(-1,  0));
                float d_r  = SampleLinearDepth(i.uv + texelSize * float2( 1,  0));
                float d_t  = SampleLinearDepth(i.uv + texelSize * float2( 0,  1));
                float d_b  = SampleLinearDepth(i.uv + texelSize * float2( 0, -1));

                float gx = -d_tl - 2*d_l - d_bl + d_tr + 2*d_r + d_br;
                float gy = -d_tl - 2*d_t - d_tr + d_bl + 2*d_b + d_br;
                float depthEdge = sqrt(gx * gx + gy * gy) * _DepthSensitivity * 0.05;

                // ---- Normal-based edge: catches interior creases (like a hard
                // edge down the middle of a cube) that depth alone would miss ----
                float3 n_c = ReconstructNormal(i.uv, texelSize);
                float3 n_l = ReconstructNormal(i.uv + texelSize * float2(-1, 0), texelSize);
                float3 n_r = ReconstructNormal(i.uv + texelSize * float2( 1, 0), texelSize);
                float3 n_t = ReconstructNormal(i.uv + texelSize * float2(0,  1), texelSize);
                float3 n_b = ReconstructNormal(i.uv + texelSize * float2(0, -1), texelSize);

                float normalEdge = (1.0 - dot(n_c, n_l)) + (1.0 - dot(n_c, n_r))
                                  + (1.0 - dot(n_c, n_t)) + (1.0 - dot(n_c, n_b));
                normalEdge *= _NormalSensitivity;

                float edge = saturate(depthEdge + normalEdge);
                edge = smoothstep(_EdgeSoftness, 1.0, edge);

                // ---- Rough/hand-inked break-up: sample a tiling noise texture
                // and use it to make the line waver in thickness/opacity rather
                // than reading as a perfectly clean vector line ----
                half roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_RoughnessTex,
                                                   i.uv * _RoughnessScale).r;
                float roughEdge = edge * lerp(1.0, roughness, _RoughnessAmount);

                half4 outCol = lerp(sceneColor, _OutlineColor, saturate(roughEdge));
                return outCol;
            }
            ENDHLSL
        }
    }
}
