Shader "Custom/HorizonFog"
{
    Properties
    {
        _FogColor ("Cor da Névoa", Color) = (0.5, 0.5, 0.5, 1)
        
        // Controla onde a névoa começa a sumir e onde ela some de vez (Baseado no UV do objeto)
        _GradientStart ("Início do Fade (Altura)", Range(0, 1)) = 0.2
        _GradientEnd ("Fim do Fade (Altura)", Range(0, 1)) = 0.8
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
            float _GradientStart;
            float _GradientEnd;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Pega a UV real do cilindro
                o.uv = v.uv; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // A MÁGICA CONSERTADA TÁ AQUI:
                // Cria a máscara de degradê baseada na UV.y (altura física do modelo 3D)
                float alphaMask = 1.0 - smoothstep(_GradientStart, _GradientEnd, i.uv.y);
                
                // Retorna a cor sólida do Fog com o Alpha calculado
                return float4(_FogColor.rgb, _FogColor.a * alphaMask);
            }
            ENDCG
        }
    }
}