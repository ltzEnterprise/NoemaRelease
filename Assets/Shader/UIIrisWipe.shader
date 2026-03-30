Shader "UI/IrisWipe"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0,0,0,1)
        _Radius ("Radius", Range(0, 1.5)) = 1.0
        _CenterX ("Center X", Range(0, 1)) = 0.5
        _CenterY ("Center Y", Range(0, 1)) = 0.5
        _AspectRatio ("Aspect Ratio", Float) = 1.77
        
        // Propriedades exigidas pela UI da Unity para funcionar em Canvas
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Radius;
            float _CenterX;
            float _CenterY;
            float _AspectRatio;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Configura o centro (0.5, 0.5 é o meio da tela)
                float2 center = float2(_CenterX, _CenterY);
                float2 pos = IN.texcoord;

                // Ajusta a matemática para o círculo não ficar oval (Aspect Ratio)
                pos.x = (pos.x - center.x) * _AspectRatio + center.x;

                // Calcula a distância do pixel atual até o centro
                float dist = distance(pos, center);

                // A MÁGICA:
                // Se a distância for menor que o Raio, fica 100% Transparente (o buraco).
                // Se for maior, desenha a cor (preto).
                if (dist < _Radius)
                {
                    return fixed4(0,0,0,0); 
                }

                return IN.color;
            }
            ENDCG
        }
    }
}