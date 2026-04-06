Shader "Custom/HorizonFog"
{
    Properties
    {
        _FogColor ("Cor da Névoa", Color) = (0.5, 0.5, 0.5, 1)
        
        // Agora é uma FAIXA, não um balde que enche até o chão
        _FadeBottom ("Some no Chão (Zero Embaixo)", Range(0, 1)) = 0.1
        _HorizonLine ("Linha do Horizonte (Pico da Névoa)", Range(0, 1)) = 0.2
        _FadeTop ("Some no Céu (Zero em Cima)", Range(0, 1)) = 0.8
        
        // Mantido pro seu DayNightCycle continuar funcionando
        _DensityMultiplier ("Multiplicador de Densidade", Range(0, 5)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-1" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _FogColor;
            float _FadeBottom;
            float _HorizonLine;
            float _FadeTop;
            float _DensityMultiplier;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Faz a névoa sumir do horizonte pra baixo (salva o seu chão)
                float fadeBaixo = smoothstep(_FadeBottom, _HorizonLine, i.uv.y);

                // Faz a névoa sumir do horizonte pra cima (salva o seu céu)
                float fadeCima = 1.0 - smoothstep(_HorizonLine, _FadeTop, i.uv.y);

                // Multiplica os dois pra criar uma "Faixa" de neblina só no fundo
                float alphaMask = fadeBaixo * fadeCima;
                
                // Suaviza a curva pra ficar mais atmosférico
                alphaMask = pow(alphaMask, 1.5);
                
                // Aplica a densidade global do script de Dia/Noite
                float finalAlpha = clamp(_FogColor.a * alphaMask * _DensityMultiplier, 0.0, 1.0);
                
                return float4(_FogColor.rgb, finalAlpha);
            }
            ENDCG
        }
    }
}