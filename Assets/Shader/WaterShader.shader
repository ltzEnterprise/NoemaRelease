Shader "Custom/AguaNoemaDefinitiva"
{
    Properties
    {
        [Header(Cores e Profundidade)]
        _CorFunda ("Cor Fundo do Mar", Color) = (0.02, 0.1, 0.15, 0.95)
        _CorRasa ("Cor Beirada (Areia)", Color) = (0.1, 0.4, 0.4, 0.5)
        _DistanciaTransparencia ("Fade da Areia", Range(0.1, 20.0)) = 3.0

        [Header(1. ANIMACAO FISICA (Malha 3D))]
        _AlturaOndaFisica ("Altura da Onda 3D", Range(0.0, 5.0)) = 0.2
        _FrequenciaOnda ("Frequência da Onda 3D", Range(0.1, 10.0)) = 1.0

        [Header(2. A ESPUMA INDESTRUTIVEL)]
        _CorEspuma ("Cor da Espuma", Color) = (1.0, 1.0, 1.0, 1.0)
        _TamanhoEspuma ("Volume da Espuma", Range(0.01, 10.0)) = 1.0
        _CorteEspuma ("Faca (Linha Dura)", Range(0.0, 1.0)) = 0.5
        _DistorcaoEspuma ("Distorção nas Ondas", Range(0.0, 3.0)) = 1.0
        
        [Header(3. ONDA DA MARE (Pulsar)]
        _ForcaMare ("Força do Vai e Vem", Range(0.0, 2.0)) = 0.5
        _VelocidadeMare ("Velocidade da Maré", Range(0.0, 5.0)) = 1.0

        [Header(Ondas e Relevo)]
        [Normal] _NormalMap ("GAVETA DO NORMAL MAP", 2D) = "bump" {}
        _ForcaNormal ("Força do Relevo", Range(0.0, 5.0)) = 1.0
        _TamanhoOnda ("Tamanho da Textura", Float) = 15.0
        _VelocidadeOnda ("Velocidade (X, Y)", Vector) = (0.05, 0.02, 0.0, 0.0)

        [Header(Iluminacao e Espelho)]
        _BrilhoSol ("Força do Brilho do Sol", Range(0, 10)) = 2.0
        _Smoothness ("Foco do Brilho", Range(10, 500)) = 300.0
        _ForcaReflexo ("Força do Espelho", Range(0, 1)) = 0.5
        _DistorcaoReflexo ("Distorção do Reflexo", Range(0.0, 0.2)) = 0.05
        _FresnelPower ("Foco no Horizonte", Range(1.0, 10.0)) = 5.0
        _OndaNaSombra ("Visibilidade da Onda na Sombra", Range(0.0, 0.5)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma require depth_texture
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float4 screenPos : TEXCOORD1; float2 uv : TEXCOORD3; float3 viewDirWS : TEXCOORD4; float3 tangentWS : TEXCOORD5; float3 bitangentWS : TEXCOORD6; float3 normalWS : TEXCOORD7; };

            CBUFFER_START(UnityPerMaterial)
                half4 _CorFunda; half4 _CorRasa; float _DistanciaTransparencia; 
                float _AlturaOndaFisica; float _FrequenciaOnda;
                half4 _CorEspuma; float _TamanhoEspuma; float _CorteEspuma; float _DistorcaoEspuma;
                float _ForcaMare; float _VelocidadeMare;
                float _ForcaNormal; float _TamanhoOnda; float4 _VelocidadeOnda; 
                float _BrilhoSol; float _Smoothness; float _ForcaReflexo; float _DistorcaoReflexo; float _FresnelPower; float _OndaNaSombra;
            CBUFFER_END

            TEXTURE2D(_NormalMap);          SAMPLER(sampler_NormalMap);
            TEXTURE2D(_TexturaReflexoAgua); SAMPLER(sampler_TexturaReflexoAgua);

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                float tempo = _Time.y;
                posWS.y += sin(posWS.x * _FrequenciaOnda + tempo) * _AlturaOndaFisica;
                posWS.y += cos(posWS.z * _FrequenciaOnda * 0.8 + tempo * 1.2) * (_AlturaOndaFisica * 0.5);

                output.positionWS = posWS; output.positionCS = TransformWorldToHClip(posWS); output.screenPos = ComputeScreenPos(output.positionCS); output.viewDirWS = GetCameraPositionWS() - posWS; output.uv = posWS.xz / _TamanhoOnda;
                output.tangentWS = normalInput.tangentWS; output.bitangentWS = normalInput.bitangentWS; output.normalWS = normalInput.normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float tempo = _Time.y;
                float2 uv1 = input.uv + tempo * _VelocidadeOnda.xy; 
                float2 uv2 = input.uv - tempo * (_VelocidadeOnda.xy * 0.5);

                half4 map1 = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1); 
                half4 map2 = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2);
                half3 normalCruzada = UnpackNormalScale(map1, _ForcaNormal) + UnpackNormalScale(map2, _ForcaNormal);
                normalCruzada = normalize(normalCruzada);
                half3 normalReal = normalize(normalCruzada.x * input.tangentWS + normalCruzada.y * input.bitangentWS + normalCruzada.z * input.normalWS);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // --- O DEPTH ROOT E INDESTRUTÍVEL ---
                float rawDepth = 0;
                float profundidade = 100.0;
                #if defined(REQUIRE_DEPTH_TEXTURE)
                    rawDepth = SampleSceneDepth(screenUV);
                    float sceneZ = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float waterZ = input.screenPos.w;
                    profundidade = sceneZ - waterZ;
                #endif
                
                float depthFactor = saturate(profundidade / _DistanciaTransparencia);

                // 1. A Espuma Base: O quão perto a água está de QUALQUER coisa
                float forcaBase = 1.0 - saturate(profundidade / _TamanhoEspuma);
                
                // 2. A Maré e a Distorção cruzando juntas
                float animacaoMare = sin(tempo * _VelocidadeMare) * _ForcaMare;
                float limiteDistorcido = forcaBase + (normalCruzada.x * _DistorcaoEspuma) + (animacaoMare * forcaBase);
                
                // 3. A Faca
                float espumaTotal = step(_CorteEspuma, limiteDistorcido);
                
                // Impede a espuma de renderizar no céu infinito (bug clássico)
                espumaTotal *= step(0.01, profundidade);
                // ----------------------------------------

                float3 viewDir = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(normalReal, viewDir)), _FresnelPower);
                float forcaEspelhoAtual = saturate(fresnel + _OndaNaSombra) * _ForcaReflexo;

                float2 uvDistorcida = screenUV + (normalCruzada.xy * _DistorcaoReflexo);
                half4 reflexoCru = SAMPLE_TEXTURE2D(_TexturaReflexoAgua, sampler_TexturaReflexoAgua, uvDistorcida);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                half3 luzAmbiente = SampleSH(normalReal);
                half3 luzDireta = mainLight.color * saturate(dot(normalReal, mainLight.direction)) * mainLight.shadowAttenuation;
                float specularTerm = pow(saturate(dot(normalReal, normalize(mainLight.direction + viewDir))), _Smoothness) * _BrilhoSol * mainLight.shadowAttenuation; 

                half4 corAgua = lerp(_CorRasa, _CorFunda, depthFactor); 
                corAgua.rgb *= (luzDireta * 0.3 + luzAmbiente); 

                half3 corFinalRGB = lerp(corAgua.rgb, reflexoCru.rgb, forcaEspelhoAtual);
                corFinalRGB += (mainLight.color * specularTerm);
                
                corFinalRGB = lerp(corFinalRGB, _CorEspuma.rgb * (luzDireta * 0.7 + luzAmbiente + 0.3), espumaTotal);
                
                return half4(corFinalRGB, saturate(corAgua.a + espumaTotal));
            }
            ENDHLSL
        }
    }
}