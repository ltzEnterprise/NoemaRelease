Shader "Custom/LucidDreamAtmosphere"
{
    Properties
    {
        [Header(Controle Geral)]
        _Intensity ("Força Global do Efeito", Range(0.0, 1.0)) = 1.0 

        [Header(Compressao de Sombras)]
        _ShadowLift ("Elevação de Pretos", Range(0.0, 0.5)) = 0.15
        _ShadowTint ("Cor do Preto (Ex: Azul/Roxo escuro)", Color) = (0.05, 0.06, 0.1, 1.0)

        [Header(Halation Optico (Vazamento Suave))]
        _HalationSpread ("Raio do Halation (Pixels)", Range(0.0, 10.0)) = 4.0
        _HalationIntensity ("Intensidade do Vazamento", Range(0.0, 2.0)) = 0.8
        _HalationThreshold ("Brilho Mínimo para Halation (Menor = Mais glow)", Range(0.0, 2.0)) = 0.5

        [Header(Micro Drift Temporal e Croma)]
        _ChromaSpread ("Separação Cromática (Pixels)", Range(0.0, 5.0)) = 1.5
        _DriftSpeed ("Velocidade do Pulso Onírico", Range(0.0, 10.0)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "LucidDreamPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float _Intensity;
            float _ShadowLift;
            float4 _ShadowTint;
            
            float _HalationSpread;
            float _HalationIntensity;
            float _HalationThreshold;
            
            float _ChromaSpread;
            float _DriftSpeed;

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.uv;
                
                float2 texelSize = 1.0 / _ScreenParams.xy;

                // 1. TEMPORAL DRIFT E CROMA
                float2 drift = float2(sin(_Time.y * _DriftSpeed), cos(_Time.y * _DriftSpeed * 0.8));
                float2 subpixelOffset = drift * (texelSize * 0.5); 

                float2 uvR = uv + subpixelOffset + (texelSize * _ChromaSpread * float2(1, 0));
                float2 uvG = uv + subpixelOffset; 
                float2 uvB = uv + subpixelOffset - (texelSize * _ChromaSpread * float2(0, 1));

                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvR).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvG).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvB).b;
                
                // Cor original da câmera (limpa)
                half3 rawColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv).rgb;
                
                // Cor com efeito de croma/drift
                half3 dreamColor = half3(r, g, b);

                // 2. HALATION
                float2 offsets[4] = { float2(1,1), float2(-1,-1), float2(-1,1), float2(1,-1) };
                half3 halation = 0;
                
                for(int i = 0; i < 4; i++) 
                {
                    half3 tap = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvG + offsets[i] * texelSize * _HalationSpread).rgb;
                    halation += max(0.0, tap - _HalationThreshold);
                }
                
                dreamColor += (halation * 0.25) * _HalationIntensity;

                // 3. COMPRESSÃO DE SOMBRAS
                float luminance = dot(dreamColor, float3(0.299, 0.587, 0.114));
                float shadowMask = saturate(1.0 - (luminance * 2.0)); 
                dreamColor = lerp(dreamColor, dreamColor + _ShadowTint.rgb, shadowMask * _ShadowLift);

                // 4. MISTURA FINAL (Controlada pelo slider _Intensity)
                half3 finalColor = lerp(rawColor, dreamColor, _Intensity);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}