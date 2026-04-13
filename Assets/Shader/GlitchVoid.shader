Shader "Custom/GlitchVoidFinalLegivel"
{
    Properties
    {
        [Header(Glitch e Cor)]
        _Intensity ("Força do Rasgo", Range(0, 5)) = 0.0
        _Speed ("Velocidade dos Ticks", Range(0, 50)) = 15.0
        _BlockSize ("Tamanho dos Blocos", Range(0.1, 10)) = 2.0
        _ColorTint ("Cor do Mundo (Seu Verde)", Color) = (0, 1, 0, 1)
        
        [Header(Iluminacao)]
        _SkyDarkness ("Escuridão do Céu (1 = Breu)", Range(0, 1)) = 0.95

        [Header(Destruicao de Qualidade MODO MAXIMO)]
        _Pixelation ("Resolução no Glitch Maximo", Float) = 256
        _ColorSteps ("Cores no Glitch Maximo", Float) = 8
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

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float _Speed;
                float _BlockSize;
                float4 _ColorTint;
                float _SkyDarkness;
                float _Pixelation;
                float _ColorSteps;
            CBUFFER_END

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
                
                float2 uvOriginal = input.uv;

                // SE A INTENSIDADE FOR ZERO, DEVOLVE A TELA 100% LIMPA
                if (_Intensity <= 0.01)
                {
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvOriginal);
                }

                float rawDepth = SampleSceneDepth(uvOriginal);
                float linearDepth = Linear01Depth(rawDepth, _ZBufferParams);

                // Normaliza a força do glitch de 0 a 1
                float forcaGeral = saturate(_Intensity / 5.0);

                if (linearDepth > 0.999) 
                {
                    half3 skyColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvOriginal).rgb;
                    return half4(skyColor * (1.0 - (_SkyDarkness * forcaGeral)), 1.0);
                }

                float3 worldPos = ComputeWorldSpacePosition(uvOriginal, rawDepth, UNITY_MATRIX_I_VP);
                
                // PIXELIZAÇÃO GRADUAL
                float pixelAtual = lerp(3000.0, _Pixelation, forcaGeral);
                float2 uvPixelada = floor(uvOriginal * pixelAtual) / pixelAtual;

                float timeTicks = floor(_Time.y * _Speed);
                float blocoY = floor(worldPos.y * _BlockSize);
                
                float noise = hash(float2(blocoY, timeTicks));
                float distorcao = 0.0;
                
                if (noise > 0.7) 
                {
                    distorcao = (noise - 0.5) * (_Intensity * 0.1); 
                }

                float aberra = _Intensity * 0.01;
                float2 uvDistorcida = uvPixelada + float2(distorcao, 0);

                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvDistorcida + float2(aberra, 0)).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvDistorcida).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvDistorcida - float2(aberra, 0)).b;

                half4 finalColor = half4(r, g, b, 1.0);
                
                // ESCURECIMENTO GRADUAL (Começa na claridade original 1.0 e cai até 0.3)
                float brilhoAtual = lerp(1.0, 0.3, forcaGeral);
                finalColor.rgb *= brilhoAtual; 

                // TINGE GRADUAL
                finalColor.rgb = lerp(finalColor.rgb, finalColor.rgb * _ColorTint.rgb * 2.0, _ColorTint.a * forcaGeral);

                // DESTRUIÇÃO DE COR GRADUAL
                float stepsAtual = lerp(256.0, _ColorSteps, forcaGeral);
                finalColor.rgb = floor(finalColor.rgb * stepsAtual) / stepsAtual;

                return finalColor; 
            }
            ENDHLSL
        }
    }
}