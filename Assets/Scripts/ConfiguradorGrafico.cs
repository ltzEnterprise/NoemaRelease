using UnityEngine;
using UnityEngine.Rendering; 

public class ConfiguradorGrafico : MonoBehaviour
{
    [Header("--- ARRASTE OS ARQUIVOS DO ASSET AQUI ---")]
    public Material skyboxMaterial; // O MATERIAL do céu (não a textura)
    public VolumeProfile volumeProfile; // O arquivo Night_URP

    [Header("--- ARRASTE OS OBJETOS DA CENA AQUI ---")]
    public Light minhaLuzDirectional; 
    public Volume meuGlobalVolume;    

    [Header("--- CONFIGURAÇÃO DA LUA ---")]
    public Color corDaLua = new Color(0.1f, 0.1f, 0.3f); 
    public float intensidadeDaLua = 0.2f; 

    void Start() // <--- AGORA ELE RODA SOZINHO NO PLAY
    {
        AplicarGraficos();
    }

    // Também roda se você clicar com botão direito pra testar sem dar play
    [ContextMenu("FORÇAR GRAFICOS AGORA")]
    public void AplicarGraficos()
    {
        // 1. Aplica o Skybox
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            DynamicGI.UpdateEnvironment();
        }

        // 2. Aplica o Volume (Neblina)
        if (meuGlobalVolume != null && volumeProfile != null)
        {
            meuGlobalVolume.profile = volumeProfile;
        }

        // 3. Configura a Luz
        if (minhaLuzDirectional != null)
        {
            minhaLuzDirectional.color = corDaLua;
            minhaLuzDirectional.intensity = intensidadeDaLua;
            
            // Garante que a luz projete sombras
            minhaLuzDirectional.shadows = LightShadows.Soft;
        }
    }
}