Shader "Custom/PlasmaDoorURP"
{
    Properties
    {
        _MainTex ("Textura Branca", 2D) = "white" {}
        _SecondaryTex ("Textura Azul", 2D) = "white" {}
        
        [HDR] _Color ("Cor Base", Color) = (1, 1, 1, 1)
        _PlasmaIntensity ("Intensidade do Brilho", Range(0.1, 10.0)) = 1.5
        _Opacity ("Opacidade Geral", Range(0.0, 1.0)) = 0.8
        
        _SpeedMain ("Velocidade Branca (X, Y)", Vector) = (0.02, 0.05, 0, 0)
        _SpeedSecondary ("Velocidade Azul (X, Y)", Vector) = (-0.03, -0.02, 0, 0)
        _Distortion ("Forca da Distorcao", Range(0, 1)) = 0.15
        _PulseSpeed ("Velocidade da Pulsacao", Range(0, 10)) = 1.0
        
        [HDR] _FresnelColor ("Cor da Borda (HDR)", Color) = (0, 0.5, 1, 3)
        _FresnelPower ("Espessura da Borda", Range(0.1, 10)) = 3.0

        _EdgeFade ("Suavidade do Corte (Esconde bordas)", Range(0.1, 5.0)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        
        LOD 100
        
        // CORREÇÃO: Blend de transparência tradicional. Permite usar a barra de opacidade
        // sem estourar as cores para branco puro.
        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite Off
        Cull Off 

        Pass
        {
            Name "ForwardLit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvMain : TEXCOORD0;
                float2 uvSec : TEXCOORD1;
                float2 uvOrig : TEXCOORD2; // Guarda a UV original parada para a máscara de borda
                float3 normalWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_SecondaryTex); SAMPLER(sampler_SecondaryTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _SecondaryTex_ST;
                float4 _Color;
                float _PlasmaIntensity;
                float _Opacity;
                float4 _SpeedMain;
                float4 _SpeedSecondary;
                float _Distortion;
                float _PulseSpeed;
                float4 _FresnelColor;
                float _FresnelPower;
                float _EdgeFade;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // UV original guardada sem sofrer alterações de movimento
                output.uvOrig = input.uv;
                
                float tempo = _Time.y;
                output.uvMain = TRANSFORM_TEX(input.uv, _MainTex) + _SpeedMain.xy * tempo;
                output.uvSec = TRANSFORM_TEX(input.uv, _SecondaryTex) + _SpeedSecondary.xy * tempo;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // 1. Ler a textura secundária
                half4 corSecundaria = SAMPLE_TEXTURE2D(_SecondaryTex, sampler_SecondaryTex, input.uvSec);
                
                // 2. Distorcer a UV da principal
                float2 uvDistorcida = input.uvMain + (corSecundaria.rg - 0.5) * _Distortion;
                half4 corPrincipal = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvDistorcida);
                
                // 3. A NOVA CORREÇÃO: Em vez de multiplicar (que destrói os detalhes se usares azul), 
                // vamos somá-las e dividi-las a meio. Detalhes perfeitos.
                half3 plasmaMisto = (corPrincipal.rgb + corSecundaria.rgb) * 0.5;
                
                // 4. Pulso mais gentil
                float pulso = (sin(_Time.y * _PulseSpeed) * 0.2) + 0.8; 
                
                // 5. Aplicar a cor baseada na nova barra de Intensidade
                half3 plasmaBase = plasmaMisto * _Color.rgb * pulso * _PlasmaIntensity;
                
                // 6. Efeito de borda do campo de forças (Fresnel)
                float fresnelTerm = pow(1.0 - saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS))), _FresnelPower);
                half3 brilhoBorda = fresnelTerm * _FresnelColor.rgb;
                
                // ==========================================================
                // 7. O TRUQUE PARA ESCONDER OS CORTES DA TEXTURA
                // Usamos o PI (3.14159) na UV original. Isto cria uma máscara 
                // que é 100% preta nas extremidades do teu modelo e 100% branca no meio.
                float mascaraX = sin(input.uvOrig.x * 3.14159);
                float mascaraY = sin(input.uvOrig.y * 3.14159);
                float mascaraDeBorda = pow(mascaraX * mascaraY, _EdgeFade);
                // ==========================================================

                // 8. O canal Alfa (Transparência) junta a tua barra de Opacidade com a máscara das bordas
                float alphaFinal = _Opacity * mascaraDeBorda;
                
                half3 corFinal = plasmaBase + brilhoBorda;
                
                return half4(corFinal, alphaFinal);
            }
            ENDHLSL
        }
    }
}