Shader "Custom/LowPolyTextureSimplifier"
{
    Properties
    {
        [Header("Simplificacao da Textura")]
        _ColorSteps ("Niveis de Cor (Achatamento)", Range(2.0, 32.0)) = 10.0
        
        [Header("Saturacao e Brilho")]
        _Saturation ("Saturacao Global", Range(0.0, 3.0)) = 1.3
        _Brightness ("Brilho Global", Range(0.5, 3.0)) = 1.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "TextureSimplifierPass"

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

            float _ColorSteps;
            float _Saturation;
            float _Brightness;

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

                // 1. LEITURA LIMPA (Resolucao Nativa, Sem Pixelizar)
                half3 originalColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv).rgb;

                // 2. EXTRAÇÃO DE LUMINÂNCIA
                // Guardamos a iluminação suave original do motor pra reaplicar depois
                float rawLuminance = dot(originalColor, float3(0.299, 0.587, 0.114));
                
                // 3. ACHATAMENTO DE CORES (A Mágica do "Simplificado")
                // Dividimos a cor original pelo brilho pra achar a "cor base" chapada
                half3 flattenedColor = originalColor / max(0.0001, rawLuminance);
                
                // Achata a cor base (usando Round pra manter as cores vivas)
                flattenedColor = round(flattenedColor * _ColorSteps) / _ColorSteps;
                
                // 4. REAPLICAR ILUMINAÇÃO
                // Colocamos a luz original de volta em cima da textura chapada
                half3 colorWithLight = flattenedColor * rawLuminance;
                
                // 5. SATURAÇÃO E BRILHO
                // Extrai a luminância final pra saturar corretamente
                float finalLuminance = dot(colorWithLight, float3(0.299, 0.587, 0.114));
                half3 color = lerp(half3(finalLuminance, finalLuminance, finalLuminance), colorWithLight, _Saturation);
                
                // Brilho global
                color *= _Brightness;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}