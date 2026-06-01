// Custom URP lit shader for foliage with vertex-based wind sway.
// Drop on any leaf mesh; UV.y is used as the sway height mask (bottom-pinned).
Shader "Custom/SwayingLeaves"
{
    Properties
    {
        [MainTexture] _BaseMap     ("Albedo",       2D)            = "white" {}
        [MainColor]   _BaseColor   ("Color",        Color)         = (1,1,1,1)
        _Cutoff       ("Alpha Cutoff", Range(0,1))                 = 0.4

        [Normal][NoScaleOffset]
        _BumpMap      ("Normal Map",   2D)            = "bump"  {}
        _BumpScale    ("Normal Scale", Float)                       = 1.0

        [NoScaleOffset]
        _MetallicGlossMap ("Metallic", 2D)            = "white" {}
        _Metallic     ("Metallic",     Range(0,1))                 = 0.0
        _GlossMapScale("Smoothness",   Range(0,1))                 = 0.0

        [NoScaleOffset]
        _OcclusionMap ("Occlusion",    2D)            = "white" {}
        _OcclusionStrength ("Strength",Range(0,1))                 = 1.0

        [NoScaleOffset]
        _EmissionMap  ("Emission",     2D)            = "black" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)

        [Header(Wind)]
        _SwaySpeed    ("Speed",     Range(0.1, 8))  = 1.5
        _SwayStrength ("Strength",  Range(0,   0.3))= 0.04
        _SwayFrequency("Frequency", Range(0.1, 8))  = 1.2

        // Internal URP properties (required by StandardLitShader GUI)
        [HideInInspector] _WorkflowMode     ("WorkflowMode",     Float) = 1
        [HideInInspector] _Surface          ("SurfaceType",      Float) = 0
        [HideInInspector] _Blend            ("BlendingMode",     Float) = 0
        [HideInInspector] _AlphaClip        ("AlphaClip",        Float) = 1
        [HideInInspector] _SrcBlend         ("__src",            Float) = 1
        [HideInInspector] _DstBlend         ("__dst",            Float) = 0
        [HideInInspector] _ZWrite           ("__zw",             Float) = 1
        [HideInInspector] _Cull             ("__cull",           Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "TransparentCutout"
            "Queue"           = "AlphaTest"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 300

        // ─────────────────────────────────────────────────────────────────────
        // FORWARD LIT
        // ─────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull  Off
            ZWrite On
            AlphaToMask On

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex   SwayVert
            #pragma fragment SwayFrag

            // URP multi-compile
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            // Feature keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local          _NORMALMAP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Samplers ──────────────────────────────────────────────────────
            TEXTURE2D(_BaseMap);          SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);          SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_OcclusionMap);     SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);      SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Cutoff;
                half   _BumpScale;
                half   _Metallic;
                half   _GlossMapScale;
                half   _OcclusionStrength;
                half4  _EmissionColor;
                half   _SwaySpeed;
                half   _SwayStrength;
                half   _SwayFrequency;
            CBUFFER_END

            // ── Structs ───────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS  : POSITION;
                float3 normalOS    : NORMAL;
                float4 tangentOS   : TANGENT;
                float2 texcoord    : TEXCOORD0;
                float2 lightmapUV  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                float3 positionWS  : TEXCOORD3;
                half3  normalWS    : TEXCOORD4;
                #if defined(_NORMALMAP)
                half4  tangentWS   : TEXCOORD5;
                #endif
                half3  viewDirWS   : TEXCOORD6;
                half4  fogFactorAndVertexLight : TEXCOORD7;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD8;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Wind sway ─────────────────────────────────────────────────────
            // uvY  : UV.y used as height mask so the base stays planted.
            float3 Sway(float3 wpos, float uvY)
            {
                float t  = _Time.y;
                float sx = sin(wpos.x * _SwayFrequency       + t * _SwaySpeed)       * 0.65
                         + sin(wpos.z * _SwayFrequency * 1.3 + t * _SwaySpeed * 0.8) * 0.35;
                float sz = cos(wpos.z * _SwayFrequency       + t * _SwaySpeed * 0.7) * 0.5
                         + cos(wpos.x * _SwayFrequency * 1.1 + t * _SwaySpeed * 1.2) * 0.5;
                return float3(sx, 0, sz) * (_SwayStrength * saturate(uvY));
            }

            // ── Vertex ────────────────────────────────────────────────────────
            Varyings SwayVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posIn  = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normIn = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                // Apply sway in world space
                posIn.positionWS += Sway(posIn.positionWS, IN.texcoord.y);
                OUT.positionCS    = TransformWorldToHClip(posIn.positionWS);
                OUT.positionWS    = posIn.positionWS;

                OUT.uv = TRANSFORM_TEX(IN.texcoord, _BaseMap);
                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(normIn.normalWS, OUT.vertexSH);

                OUT.normalWS = normIn.normalWS;
                #if defined(_NORMALMAP)
                OUT.tangentWS = half4(normIn.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                #endif
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(posIn.positionWS);

                half  fogFactor    = ComputeFogFactor(OUT.positionCS.z);
                half3 vertexLight  = VertexLighting(posIn.positionWS, normIn.normalWS);
                OUT.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                OUT.shadowCoord = GetShadowCoord(posIn);
                #endif

                return OUT;
            }

            // ── Fragment ──────────────────────────────────────────────────────
            half4 SwayFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Albedo + alpha clip
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 albedoAlpha = baseMap * _BaseColor;
                #if defined(_ALPHATEST_ON)
                clip(albedoAlpha.a - _Cutoff);
                #endif

                // Normal
                #if defined(_NORMALMAP)
                half4 nSample  = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv);
                half3 normalTS = UnpackNormalScale(nSample, _BumpScale);
                half3 bitangent = IN.tangentWS.w * cross(IN.normalWS, IN.tangentWS.xyz);
                half3 normalWS  = NormalizeNormalPerPixel(
                    TransformTangentToWorld(normalTS, half3x3(IN.tangentWS.xyz, bitangent, IN.normalWS)));
                #else
                half3 normalTS = half3(0, 0, 1);
                half3 normalWS = NormalizeNormalPerPixel(IN.normalWS);
                #endif

                // PBR params
                half4 mrSample  = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, IN.uv);
                half  metallic  = mrSample.r * _Metallic;
                half  smoothness = mrSample.a * _GlossMapScale;
                half  ao        = lerp(1.0h,
                                       SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, IN.uv).g,
                                       _OcclusionStrength);

                // Emission
                half3 emission = half3(0, 0, 0);
                #if defined(_EMISSION)
                emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb;
                #endif

                // Build URP surface / input data
                SurfaceData surface = (SurfaceData)0;
                surface.albedo              = albedoAlpha.rgb;
                surface.metallic            = metallic;
                surface.smoothness          = smoothness;
                surface.normalTS            = normalTS;
                surface.occlusion           = ao;
                surface.emission            = emission;
                surface.alpha               = albedoAlpha.a;
                surface.clearCoatMask       = 0;
                surface.clearCoatSmoothness = 0;

                InputData inputData = (InputData)0;
                inputData.positionWS              = IN.positionWS;
                inputData.normalWS                = normalWS;
                inputData.viewDirectionWS         = SafeNormalize(IN.viewDirWS);
                inputData.fogCoord                = InitializeInputDataFog(float4(IN.positionWS, 1), IN.fogFactorAndVertexLight.x);
                inputData.vertexLighting           = IN.fogFactorAndVertexLight.yzw;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask              = SAMPLE_SHADOWMASK(IN.lightmapUV);

                #if defined(LIGHTMAP_ON)
                inputData.bakedGI = SampleLightmap(IN.lightmapUV, half4(0,0,0,0), normalWS);
                #else
                inputData.bakedGI = SampleSHPixel(IN.vertexSH, normalWS);
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = IN.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb   = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────────────
        // SHADOW CASTER  (sway applied so shadows match lit mesh)
        // ─────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Cutoff;
                half   _BumpScale;
                half   _Metallic;
                half   _GlossMapScale;
                half   _OcclusionStrength;
                half4  _EmissionColor;
                half   _SwaySpeed;
                half   _SwayStrength;
                half   _SwayFrequency;
            CBUFFER_END

            struct ShadowAttr { float4 posOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct ShadowVary { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            float3 Sway(float3 wpos, float uvY)
            {
                float t  = _Time.y;
                float sx = sin(wpos.x * _SwayFrequency + t * _SwaySpeed) * 0.65
                         + sin(wpos.z * _SwayFrequency * 1.3 + t * _SwaySpeed * 0.8) * 0.35;
                float sz = cos(wpos.z * _SwayFrequency + t * _SwaySpeed * 0.7) * 0.5;
                return float3(sx, 0, sz) * (_SwayStrength * saturate(uvY));
            }

            ShadowVary ShadowVert(ShadowAttr IN)
            {
                ShadowVary OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 wpos    = TransformObjectToWorld(IN.posOS.xyz) + Sway(TransformObjectToWorld(IN.posOS.xyz), IN.uv.y);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - wpos);
                #else
                float3 lightDir = _LightDirection;
                #endif

                OUT.posCS = TransformWorldToHClip(ApplyShadowBias(wpos, normalWS, lightDir));
                OUT.uv    = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 ShadowFrag(ShadowVary IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                #if defined(_ALPHATEST_ON)
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────────────
        // DEPTH ONLY
        // ─────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Cutoff;
                half   _BumpScale;
                half   _Metallic;
                half   _GlossMapScale;
                half   _OcclusionStrength;
                half4  _EmissionColor;
                half   _SwaySpeed;
                half   _SwayStrength;
                half   _SwayFrequency;
            CBUFFER_END

            struct DepthAttr { float4 posOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct DepthVary { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            float3 Sway(float3 wpos, float uvY)
            {
                float t  = _Time.y;
                float sx = sin(wpos.x * _SwayFrequency + t * _SwaySpeed) * 0.65;
                float sz = cos(wpos.z * _SwayFrequency + t * _SwaySpeed * 0.7) * 0.5;
                return float3(sx, 0, sz) * (_SwayStrength * saturate(uvY));
            }

            DepthVary DepthVert(DepthAttr IN)
            {
                DepthVary OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float3 wpos = TransformObjectToWorld(IN.posOS.xyz) + Sway(TransformObjectToWorld(IN.posOS.xyz), IN.uv.y);
                OUT.posCS = TransformWorldToHClip(wpos);
                OUT.uv    = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half DepthFrag(DepthVary IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                #if defined(_ALPHATEST_ON)
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a - _Cutoff);
                #endif
                return IN.posCS.z;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
