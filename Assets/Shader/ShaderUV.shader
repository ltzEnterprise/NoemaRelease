Shader "Custom/HiddenReveal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _GlowColor ("Glow Color", Color) = (0.5, 0, 1, 1)
        _LightAngle ("Light Angle", Range(0, 1)) = 0.8 // Diminui um pouco pra testar
        _LightRange ("Light Range", Float) = 10
        
        // Valores internos
        _LightPosition ("Light Position", Vector) = (0,0,0,0)
        _LightDirection ("Light Direction", Vector) = (0,0,1,0)
        _LightState ("Light State", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline" = "UniversalPipeline" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        // Mantive o Offset pq é boa prática pra decalques, 
        // mas o foco aqui é a precisão matemática abaixo
        Offset -1, -1

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
            };

            sampler2D _MainTex;
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _GlowColor;
                float3 _LightPosition;
                float3 _LightDirection;
                float _LightAngle;
                float _LightRange;
                float _LightState;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            // Mudei para float4 (Alta precisão) para parar de tremer
            float4 frag (Varyings IN) : SV_Target
            {
                float4 col = tex2D(_MainTex, IN.uv);
                
                // Cálculos em Alta Precisão (float)
                float3 pixelPos = IN.positionWS;
                float3 lightPos = _LightPosition;
                float3 dirToPixel = pixelPos - lightPos;
                
                float dist = length(dirToPixel);
                float3 dirToPixelNorm = normalize(dirToPixel);
                float3 lightDirNorm = normalize(_LightDirection);

                // Produto Escalar
                float dotProd = dot(lightDirNorm, dirToPixelNorm);
                
                // Mudei a suavização: 0.1 de margem para evitar borda dura que pisca
                float angleMask = smoothstep(_LightAngle, _LightAngle + 0.1, dotProd);
                
                // Fade de distância mais suave
                float distMask = 1.0 - smoothstep(_LightRange * 0.7, _LightRange, dist);
                
                float finalAlpha = col.a * angleMask * distMask * _LightState;

                // Debug visual: Se ficar preto, é problema de Alpha
                return float4(col.rgb * _GlowColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}