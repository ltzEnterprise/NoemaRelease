Shader "Custom/ScannerPointillism"
{
    Properties
    {
        [Header(Cores por Profundidade)]
        [HDR] _NearColor ("Cor Próxima (Ex: Vermelho)", Color) = (1.0, 0.1, 0.0, 1.0)
        [HDR] _MidColor ("Cor Intermediária (Ex: Verde)", Color) = (0.5, 1.0, 0.0, 1.0)
        [HDR] _FarColor ("Cor Distante (Ex: Azul)", Color) = (0.0, 0.5, 1.0, 1.0)
        _MaxDistance ("Distância Máxima do Fade", Range(5, 500)) = 50.0

        [Header(Configuracao dos Pontos)]
        _NoiseScale ("Escala da Grade (Densidade Espacial)", Range(10, 500)) = 150.0
        _DotDensity ("Quantidade de Pontos (Perto)", Range(0.0, 1.0)) = 0.2
        _DistDensityMult ("Aumento de Pontos ao Longe", Range(0.0, 1.0)) = 0.4
        _DotSharpness ("Tamanho/Nitidez do Ponto", Range(0.1, 10.0)) = 3.0

        [Header(Atmosfera e Mistura)]
        _BaseGlow ("Silhueta Base (Preenche o escuro)", Range(0.0, 1.0)) = 0.1
        _DarkIntensity ("Intensidade do Escuro (1 = Breu)", Range(0.0, 1.0)) = 1.0
        _BlendOriginal ("Misturar com Cor Original", Range(0.0, 1.0)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "ScannerVolumePass"

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

            // Variáveis
            float4 _NearColor;
            float4 _MidColor;
            float4 _FarColor;
            float _MaxDistance;
            
            float _NoiseScale;
            float _DotDensity;
            float _DistDensityMult;
            float _DotSharpness;
            
            float _BaseGlow;
            float _DarkIntensity;
            float _BlendOriginal;

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
                
                // 1. LEITURA DE PROFUNDIDADE
                float2 uv = input.uv;
                float rawDepth = SampleSceneDepth(uv);
                float linearDepth = Linear01Depth(rawDepth, _ZBufferParams);

                // Retorna preto absoluto se for o Céu (fundo do cenário)
                if (linearDepth > 0.999) 
                {
                    return half4(0, 0, 0, 1);
                }

                // 2. RECONSTRUÇÃO 3D (O Segredo para grudar os pontos)
                float3 worldPos = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float distToCam = distance(_WorldSpaceCameraPos, worldPos);

                // 3. GRADIENTE DE COR POR DISTÂNCIA
                // Cria um valor (t) de 0.0 a 1.0 baseado na distância do jogador
                float t = saturate(distToCam / _MaxDistance);
                
                // Interpola Red -> Green -> Blue
                half3 colorNearMid = lerp(_NearColor.rgb, _MidColor.rgb, saturate(t * 2.0));
                half3 colorMidFar  = lerp(_MidColor.rgb, _FarColor.rgb, saturate((t - 0.5) * 2.0));
                half3 depthColor   = t < 0.5 ? colorNearMid : colorMidFar;

                // 4. MATEMÁTICA DO PONTILHADO (Grade de Voxels)
                float3 gridPos = worldPos * _NoiseScale;
                float3 voxelId = floor(gridPos);
                float3 voxelCenter = voxelId + 0.5;
                
                // Distância do pixel até o centro do seu "bloco/voxel" atual
                float distToVoxelCenter = length(gridPos - voxelCenter);
                
                // Gera um número pseudo-aleatório (0 a 1) estático para cada bloco 3D
                float noise = frac(sin(dot(voxelId, float3(12.9898, 78.233, 37.719))) * 43758.5453);
                
                // Aumenta a quantidade de pontos nos objetos mais distantes
                float currentDensity = saturate(_DotDensity + (t * _DistDensityMult));
                
                // Máscara: este bloco 3D deve conter um ponto?
                float hasDot = step(1.0 - currentDensity, noise);
                
                // Desenha a "bolinha" suave a partir do centro do bloco
                float dotShape = saturate(1.0 - (distToVoxelCenter * _DotSharpness));
                float finalDot = dotShape * hasDot;

                // 5. COMPOSIÇÃO DE COR E ATMOSFERA
                half3 dotColor = depthColor * finalDot;
                
                // Mantém as silhuetas suavemente visíveis mesmo no escuro
                half3 silhouetteColor = depthColor * _BaseGlow;
                
                // A cor final do "scanner"
                half3 scannerEffect = dotColor + silhouetteColor;

                // Pega a textura original do jogo
                half4 originalScene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);

                // Mistura com o mundo original dependendo do Slider
                half3 outputColor = lerp(scannerEffect, originalScene.rgb, _BlendOriginal);

                // Aplica a escuridão geral (cobre áreas vazias)
                outputColor = lerp(outputColor, outputColor * (1.0 - _DarkIntensity), 1.0 - saturate(finalDot + _BaseGlow + _BlendOriginal));

                return half4(outputColor, 1.0);
            }
            ENDHLSL
        }
    }
}