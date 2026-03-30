Shader "Custom/GlitchDigitalFinalLegivel"
{
    Properties
    {
        [Header(Glitch e Cor)]
        _Intensity ("Força do Rasgo", Range(0, 5)) = 1.0
        _Speed ("Velocidade dos Ticks", Range(0, 50)) = 15.0
        _BlockSize ("Tamanho dos Blocos", Range(0.1, 10)) = 2.0
        _ColorTint ("Cor do Mundo (Seu Verde)", Color) = (0, 1, 0, 1)
        
        [Header(Iluminacao)]
        _SkyDarkness ("Escuridão do Céu (1 = Breu)", Range(0, 1)) = 0.95
        _WorldBrightness ("Claridade do Mundo", Range(0, 3)) = 1.2

        [Header(Destruicao de Qualidade)]
        _Pixelation ("Resolução (Maior = Mais nítido)", Range(256, 4096)) = 1080
        _ColorSteps ("Qualidade da Cor (Menor = Pior)", Range(2, 256)) = 16
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "GlitchWorldPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
            float _Speed;
            float _BlockSize;
            float4 _ColorTint;
            float _SkyDarkness;
            float _WorldBrightness;
            float _Pixelation;
            float _ColorSteps;

            float hash(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

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
                
                // 1. LÊ TUDO LIMPO PRIMEIRO (Evita o bug do espaguete da sua foto)
                float2 uvOriginal = input.uv;
                float rawDepth = SampleSceneDepth(uvOriginal);
                float linearDepth = Linear01Depth(rawDepth, _ZBufferParams);

                // Proteção do Céu
                if (linearDepth > 0.999) 
                {
                    half3 skyColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvOriginal).rgb;
                    return half4(skyColor * (1.0 - _SkyDarkness), 1.0);
                }

                // Reconstrói 3D com precisão máxima
                float3 worldPos = ComputeWorldSpacePosition(uvOriginal, rawDepth, UNITY_MATRIX_I_VP);
                
                // 2. APLICA A PIXELIZAÇÃO SÓ NA LEITURA DA COR
                float2 uvPixelada = floor(uvOriginal * _Pixelation) / _Pixelation;

                // Rasgo Digital usando a altura real das paredes
                float timeTicks = floor(_Time.y * _Speed);
                float blocoY = floor(worldPos.y * _BlockSize);
                
                float noise = hash(float2(blocoY, timeTicks));
                float distorcao = 0.0;
                if (noise > 0.7) 
                {
                    distorcao = (noise - 0.5) * _Intensity * 0.05;
                }

                float2 uvDistorcida = uvPixelada + float2(distorcao, 0);

                // Separação de RGB
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvDistorcida + float2(distorcao, 0)).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvDistorcida).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvDistorcida - float2(distorcao, 0)).b;

                half4 finalColor = half4(r, g, b, 1.0);
                finalColor.rgb *= _WorldBrightness; 

                // Tinge o mundo
                finalColor.rgb = lerp(finalColor.rgb, finalColor.rgb * _ColorTint.rgb * 2.0, _ColorTint.a);

                // Destruição de cor (Banding)
                finalColor.rgb = floor(finalColor.rgb * _ColorSteps) / _ColorSteps;

                return finalColor; 
            }
            ENDHLSL
        }
    }
}