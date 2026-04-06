Shader "Custom/ExactGrassURP"
{
    Properties
    {
        [Header(Original Textures)]
        _BaseMap("Base Map (RGB) Alpha (A)", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _MaskMap("Mask Map (R=Smooth, G=Metal)", 2D) = "white" {}
        _ThicknessMap("Thickness Map", 2D) = "white" {}

        [Header(Surface Settings)]
        _Cutoff("Alpha Clipping", Range(0.0, 1.0)) = 0.5
        _NormalStrength("Normal Strength", Range(0.0, 2.0)) = 1.0

        [Header(Wind Physics)]
        _WindSpeed("Wind Speed", Float) = 2.0
        _WindIntensity("Wind Intensity", Float) = 0.1
        _WindWavelength("Wind Wavelength", Float) = 10.0

        [Header(Distance Fade)]
        _DistanceFadeStart("Distance Fade Start", Float) = 50.0
        _DistanceFadeEnd("Distance Fade End", Float) = 100.0
        _FadeBias("Fade Bias (Power)", Float) = 1.0

        [Header(Subsurface Scattering)]
        [HDR] _SSSColor("SSS Color", Color) = (0.5, 0.8, 0.2, 1)

        [Header(Day Night System)]
        [Toggle(_GRASSNORMAL_ON)] _GrassNormal("Use Up Normal (Night Mode)", Float) = 0.0

        // Hidden Terrain properties (Prevents Pink Error)
        [HideInInspector] _HealthyColor("Healthy Color", Color) = (1,1,1,1)
        [HideInInspector] _DryColor("Dry Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" "Nature"="Grass" }
        
        // Prevents pitch-black backside
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #pragma shader_feature_local _GRASSNORMAL_ON

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // A LINHA QUE EU ESQUECI E QUE QUEBROU O SHADER INTEIRO:
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl" 

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD1;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                float3 normalWS     : NORMAL;
                float3 tangentWS    : TANGENT;
                float3 bitangentWS  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);
            TEXTURE2D(_ThicknessMap); SAMPLER(sampler_ThicknessMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Cutoff;
                float _NormalStrength;
                float _WindSpeed;
                float _WindIntensity;
                float _WindWavelength;
                float _DistanceFadeStart;
                float _DistanceFadeEnd;
                float _FadeBias;
                float4 _SSSColor;
                float4 _HealthyColor;
                float4 _DryColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // Wind Math (PDF)
                float windWave = sin(_Time.y * _WindSpeed + (positionWS.x + positionWS.z) * _WindWavelength);
                float windOffset = windWave * _WindIntensity * input.uv.y; 
                positionWS.x += windOffset;
                positionWS.z += windOffset;

                output.positionWS = positionWS;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;

                // Normals and Tangents
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                #ifdef _GRASSNORMAL_ON
                    output.normalWS = float3(0, 1, 0);
                    output.tangentWS = float3(1, 0, 0);
                    output.bitangentWS = float3(0, 0, 1);
                #else
                    output.normalWS = normalInput.normalWS;
                    output.tangentWS = normalInput.tangentWS;
                    output.bitangentWS = normalInput.bitangentWS;
                #endif

                return output;
            }

            half4 frag(Varyings input, float isFrontFace : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Sample Textures
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                half thickness = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, input.uv).r;

                // Distance Fade Math (PDF)
                float distanceToCam = distance(GetCameraPositionWS(), input.positionWS);
                float fadeDivisor = max(0.0001, _DistanceFadeEnd - _DistanceFadeStart); // Prevents division by zero crash
                float fadeRaw = (distanceToCam - _DistanceFadeStart) / fadeDivisor;
                float fadeFactor = saturate(1.0 - fadeRaw); 
                fadeFactor = pow(fadeFactor, _FadeBias);
                
                baseColor.a *= fadeFactor;

                // Clip
                clip(baseColor.a - _Cutoff);

                // Normal Map
                half4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv);
                half3 normalTS = UnpackNormalScale(normalSample, _NormalStrength);
                
                float3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
                normalWS = normalize(normalWS);
                
                // Flip normal if looking at the back
                normalWS = isFrontFace > 0 ? normalWS : -normalWS;

                // PBR Setup
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    inputData.shadowCoord = float4(0,0,0,0);
                #endif

                Light mainLight = GetMainLight(inputData.shadowCoord);

                // SSS Math (PDF)
                float NdotL_SSS = dot(normalWS, mainLight.direction);
                float sssFactor = saturate(1.0 - NdotL_SSS) * thickness;
                half3 sssEmission = _SSSColor.rgb * sssFactor * mainLight.color * mainLight.shadowAttenuation;

                // Surface Data Setup
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor.rgb * input.color.rgb; 
                surfaceData.metallic = mask.g; 
                surfaceData.smoothness = mask.r; 
                surfaceData.normalTS = normalTS;
                surfaceData.emission = sssEmission; 
                surfaceData.occlusion = 1.0; // The fix for black spots
                surfaceData.alpha = 1.0;

                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                
                // Fog
                float fogFactor = ComputeFogFactor(input.positionHCS.z);
                finalColor.rgb = MixFog(finalColor.rgb, fogFactor);

                return finalColor;
            }
            ENDHLSL
        }

        // --- SHADOW CASTER ---
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float _Cutoff; float _WindSpeed; float _WindIntensity; float _WindWavelength;
                float _DistanceFadeStart; float _DistanceFadeEnd; float _FadeBias; float4 _SSSColor;
                float4 _HealthyColor; float4 _DryColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                float windWave = sin(_Time.y * _WindSpeed + (positionWS.x + positionWS.z) * _WindWavelength);
                float windOffset = windWave * _WindIntensity * input.uv.y;
                positionWS.x += windOffset;
                positionWS.z += windOffset;

                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(baseColor.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}