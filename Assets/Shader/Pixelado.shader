Shader "Custom/DistancePixelationSaturate"
{
    Properties
    {
        [Header(Configuracao de Pixelizacao)]
        _PixelNearSize ("Tamanho do Pixel (Perto)", Integer) = 4
        _PixelFarSize ("Tamanho do Pixel (Longe)", Integer) = 32
        _DistanceFactor ("Crescimento com Distancia (Suavidade)", Range(0.1, 5.0)) = 1.0
        
        [Header(Cor e Saturacao)]
        _Saturation ("Saturacao Global", Range(0.0, 3.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "DistancePixelationPass"

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

            // Textura da cena limpa (Opaque) injetada pelo Blitter
            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            // Variáveis
            int _PixelNearSize;
            int _PixelFarSize;
            float _DistanceFactor;
            float _Saturation;

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
                
                // 1. LEITURA DE PROFUNDIDADE (E CÉU)
                float2 uv = input.uv;
                
                // Lê a profundidade e lineariza para 0 (perto) a 1 (infinito)
                float rawDepth = SampleSceneDepth(uv);
                float linearDepth = Linear01Depth(rawDepth, _ZBufferParams);

                // Tratamento especial para o céu (evita bugs de pixelização infinita)
                // Usamos o tamanho máximo de pixel de longe para o céu.
                float safeDepth = linearDepth > 0.999 ? 1.0 : linearDepth;

                // 2. MATEMÁTICA DO PIXEL BASEADO EM DISTÂNCIA
                // Tamanho da tela em pixels
                float2 screenSize = _ScreenParams.xy;

                // Interpola o tamanho do pixel baseado na distância
                // O valor _DistanceFactor controla quão rápido o pixel cresce com a profundidade.
                float currentPixelSize = lerp((float)_PixelNearSize, (float)_PixelFarSize, saturate(safeDepth / _DistanceFactor));
                
                // Garante que o tamanho do pixel é no mínimo 1.0 (sem pixelização)
                currentPixelSize = max(1.0, currentPixelSize);

                // Cria uma grade baseada no tamanho do pixel atual
                float2 blocks = floor(screenSize / currentPixelSize);
                
                // Calcula as novas coordenadas UV pixelizadas
                float2 pixelatedUV = floor(uv * blocks) / blocks;

                // 3. AMOSTRAGEM DE COR PIXELIZADA
                // Amostra a cena original nas coordenadas pixelizadas
                half3 pixelatedColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, pixelatedUV).rgb;

                // 4. AJUSTE DE SATURAÇÃO
                // Calcula a luminância da cor pixelizada
                float luminance = dot(pixelatedColor, float3(0.299, 0.587, 0.114));
                
                // Aplica a saturação: lerp entre preto/branco (lum) e a cor original
                pixelatedColor = lerp(half3(luminance, luminance, luminance), pixelatedColor, _Saturation);

                return half4(pixelatedColor, 1.0);
            }
            ENDHLSL
        }
    }
}