Shader "Custom/RetroDitherEffect"
{
    Properties
    {
        [HideInInspector] _BlitTexture ("Screen Texture", 2D) = "white" {}
        _Color1 ("Cor Escura", Color) = (0,0,0,1)
        _Color2 ("Cor Clara", Color) = (0.9, 0.9, 0.8, 1)
        _DitherScale ("Escala do Granulado", Float) = 2.0
        _LightThreshold ("Nivel de Contraste", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "RetroDitherPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color1;
                float4 _Color2;
                float _DitherScale;
                float _LightThreshold;
            CBUFFER_END

            static const float ditherPattern[16] = {
                0, 8, 2, 10,
                12, 4, 14, 6,
                3, 11, 1, 9,
                15, 7, 13, 5
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(input.positionOS.xy, 0.0, 1.0);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv);
                
                // --- CORREÇÃO AQUI: saturate() impede que luzes fortes estourem o cálculo ---
                float lum = saturate(dot(col.rgb, float3(0.299, 0.587, 0.114)));

                float2 screenPixels = input.screenPos.xy / input.screenPos.w * _ScreenParams.xy;
                int x = int(screenPixels.x / _DitherScale) % 4;
                int y = int(screenPixels.y / _DitherScale) % 4;
                float ditherValue = ditherPattern[y * 4 + x] / 16.0;

                // Ajuste matemático para garantir contraste
                if ((lum + (ditherValue - 0.5)) < _LightThreshold)
                {
                    return _Color1; // Deve ser PRETO
                }
                else
                {
                    return _Color2; // Deve ser BRANCO
                }
            }
            ENDHLSL
        }
    }
}