using UnityEngine;
using System.Collections;

public class WeaponAltar : MonoBehaviour
{
    [Header("--- DEV MODE ---")]
    public bool devMode = false;

    [Header("--- CONFIG ---")]
    public int requiredRunesCount = 7; 
    public int weaponIDToUnlock = 1; 
    
    public AltarActivationCutscene cutsceneScript; 

    [Header("--- UI & VISUAL ---")]
    public GameObject runesUIPanel; 
    public GameObject visualWeapon; 
    public GameObject missingRunesText; 
    public GameObject highlightLight; 

    private bool isWeaponAvailable = false;
    private bool alreadyTaken = false;
    
    // Travas para organizar a UI sem brigar com o seu Raycast central
    private bool estaOlhando = false; 
    private bool mostrandoErro = false; 

    void Start()
    {
        if(runesUIPanel) runesUIPanel.SetActive(false); 
        if(visualWeapon) visualWeapon.SetActive(false);
        if(missingRunesText) missingRunesText.SetActive(false);
        if(highlightLight) highlightLight.SetActive(false);
    }

    public void SpawnWeaponOnAltar()
    {
        isWeaponAvailable = true;
        if(visualWeapon) visualWeapon.SetActive(true); 
        if(highlightLight) highlightLight.SetActive(true);
    }

    // --- MÉTODOS DO SEU SISTEMA DE RAYCAST CENTRAL (NOMES CORRIGIDOS) ---

    public void AoOlhar() // O seu laser chama isso aqui
    { 
        if (alreadyTaken) return;

        estaOlhando = true;

        // Só mostra o painel principal se a mensagem de erro NÃO estiver na tela
        if (runesUIPanel && !mostrandoErro) runesUIPanel.SetActive(true); 
    }
    
    public void AoSair() // O seu laser chama isso quando você vira a cara
    { 
        estaOlhando = false;
        if(runesUIPanel) runesUIPanel.SetActive(false); 
        if(missingRunesText) missingRunesText.SetActive(false);
    }

    public void Interagir() // O seu laser chama isso quando você aperta [E]
    {
        if (alreadyTaken) return;

        int runasAtuais = 0;

        if (InventarioRunas.Instance != null)
        {
            runasAtuais = InventarioRunas.Instance.runasNaMao.Count;
        }
        else if (!devMode)
        {
            Debug.LogError("ERRO CRÍTICO: 'InventarioRunas' não encontrado na cena!");
            return;
        }

        if (runasAtuais >= requiredRunesCount || devMode)
        {
            if (!isWeaponAvailable) SpawnWeaponOnAltar();

            if (!devMode && InventarioRunas.Instance != null) 
            {
                InventarioRunas.Instance.UsarTodasAsRunasNoAltar(); 
            }
            else if (devMode)
            {
                Debug.Log(">> ALTAR ACTIVATED VIA DEV MODE <<");
            }

            StartPickupProcess();
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(ShowMissingRunesWarning());
        }
    }

    void StartPickupProcess()
    {
        alreadyTaken = true;
        if(runesUIPanel) runesUIPanel.SetActive(false);
        if(missingRunesText) missingRunesText.SetActive(false);

        if (cutsceneScript != null) 
        {
            cutsceneScript.IniciarCutscene(); 
        }
        else 
        {
            FinalizeActivation();
        }
    }

    public void FinalizeActivation()
    {
        Debug.Log("Reward Delivered.");
        
        if (DayNightCycle.Instance != null) 
            DayNightCycle.Instance.ChangeTo(DayNightCycle.TimeState.Night);

        if(visualWeapon) visualWeapon.SetActive(false);
        if(highlightLight) highlightLight.SetActive(false);

        if (weaponIDToUnlock >= 0 && weaponIDToUnlock < EstadoGlobal.armasDesbloqueadas.Length)
        {
            EstadoGlobal.armasDesbloqueadas[weaponIDToUnlock] = true; 
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.TentarEquipar(weaponIDToUnlock); 
        }
    }

    IEnumerator ShowMissingRunesWarning() 
    { 
        mostrandoErro = true;
        if(runesUIPanel) runesUIPanel.SetActive(false); 
        if(missingRunesText) missingRunesText.SetActive(true); 
        
        yield return new WaitForSeconds(2f); 
        
        if(missingRunesText) missingRunesText.SetActive(false); 
        mostrandoErro = false;

        // Só liga a UI normal de novo se AINDA estiver olhando
        if (estaOlhando && runesUIPanel && !alreadyTaken)
        {
            runesUIPanel.SetActive(true);
        }
    }
}