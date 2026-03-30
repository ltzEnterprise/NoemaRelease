using UnityEngine;
using System.Collections;

public class WeaponAltar : MonoBehaviour
{
    [Header("--- DEV MODE ---")]
    public bool devMode = false;

    [Header("--- CONFIG ---")]
    public int requiredRunesCount = 7; 
    public int weaponIDToUnlock = 1; 
    
    // CORREÇÃO: Agora aponta para a classe nova em inglês
    public AltarActivationCutscene cutsceneScript; 

    [Header("--- UI & VISUAL ---")]
    public GameObject runesUIPanel; 
    public GameObject visualWeapon; 
    public GameObject missingRunesText; 
    public GameObject highlightLight; 

    private bool isWeaponAvailable = false;
    private bool alreadyTaken = false;

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

    // --- RAYCAST INTERACTION PROTOCOL ---

    public void OnLook() 
    { 
        if(runesUIPanel && !alreadyTaken) runesUIPanel.SetActive(true); 
    }
    
    public void OnLookAway() 
    { 
        if(runesUIPanel) runesUIPanel.SetActive(false); 
    }

    public void Interact()
    {
        if (alreadyTaken) return;

        // Verifica se tem InventarioRunas na cena (evita erro se não tiver)
        if (InventarioRunas.Instance == null)
        {
            Debug.LogError("ERRO CRÍTICO: 'InventarioRunas' não encontrado na cena!");
            return;
        }

        bool hasEnoughRunes = InventarioRunas.Instance.runasNaMao.Count >= requiredRunesCount;

        if (hasEnoughRunes || devMode)
        {
            if (!isWeaponAvailable) SpawnWeaponOnAltar();

            if (!devMode) 
            {
                InventarioRunas.Instance.UsarTodasAsRunasNoAltar(); 
            }
            else 
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
            // Se não tiver cutscene, finaliza direto
            FinalizeActivation();
        }
    }

    public void FinalizeActivation()
    {
        Debug.Log("Reward Delivered.");
        
        // Change Day/Night
        if (DayNightCycle.Instance != null) 
            DayNightCycle.Instance.ChangeTo(DayNightCycle.TimeState.Night);

        // Hide Altar Visuals
        if(visualWeapon) visualWeapon.SetActive(false);
        if(highlightLight) highlightLight.SetActive(false);

        // Unlock Weapon in Global State
        if (weaponIDToUnlock >= 0 && weaponIDToUnlock < EstadoGlobal.armasDesbloqueadas.Length)
        {
            EstadoGlobal.armasDesbloqueadas[weaponIDToUnlock] = true; 
        }

        // Force Equip via Inventory
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.TentarEquipar(weaponIDToUnlock); 
        }
    }

    IEnumerator ShowMissingRunesWarning() 
    { 
        if(missingRunesText) missingRunesText.SetActive(true); 
        yield return new WaitForSeconds(2f); 
        if(missingRunesText) missingRunesText.SetActive(false); 
    }
}