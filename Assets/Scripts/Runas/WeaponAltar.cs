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
    
    private bool estaOlhando = false; 
    private bool mostrandoErro = false; 

    void Start()
    {
        if (runesUIPanel) runesUIPanel.SetActive(false); 
        if (visualWeapon) visualWeapon.SetActive(false);
        if (missingRunesText) missingRunesText.SetActive(false);
        if (highlightLight) highlightLight.SetActive(false);

        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso
        );

        alreadyTaken = PersistenciaManager.Instance.ObterEstado("WeaponAltar_Weapon_" + weaponIDToUnlock + "_Taken", false);

        if (alreadyTaken)
        {
            if (visualWeapon) visualWeapon.SetActive(false);
            if (highlightLight) highlightLight.SetActive(false);
            if (runesUIPanel) runesUIPanel.SetActive(false);
            if (missingRunesText) missingRunesText.SetActive(false);

            if (weaponIDToUnlock >= 0 && weaponIDToUnlock < EstadoGlobal.armasDesbloqueadas.Length)
                EstadoGlobal.armasDesbloqueadas[weaponIDToUnlock] = true;
        }
    }

    public void SpawnWeaponOnAltar()
    {
        if (alreadyTaken) return;

        isWeaponAvailable = true;

        if (visualWeapon) visualWeapon.SetActive(true); 
        if (highlightLight) highlightLight.SetActive(true);
    }

    public void AoOlhar()
    { 
        if (alreadyTaken) return;

        estaOlhando = true;

        if (runesUIPanel && !mostrandoErro)
            runesUIPanel.SetActive(true); 
    }
    
    public void AoSair()
    { 
        estaOlhando = false;

        if (runesUIPanel) runesUIPanel.SetActive(false); 
        if (missingRunesText) missingRunesText.SetActive(false);
    }

    public void Interagir()
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
            if (!isWeaponAvailable)
                SpawnWeaponOnAltar();

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

        if (runesUIPanel) runesUIPanel.SetActive(false);
        if (missingRunesText) missingRunesText.SetActive(false);

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

        if (visualWeapon) visualWeapon.SetActive(false);
        if (highlightLight) highlightLight.SetActive(false);

        if (weaponIDToUnlock >= 0 && weaponIDToUnlock < EstadoGlobal.armasDesbloqueadas.Length)
        {
            EstadoGlobal.armasDesbloqueadas[weaponIDToUnlock] = true; 
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.TentarEquipar(weaponIDToUnlock); 
        }

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("WeaponAltar_Weapon_" + weaponIDToUnlock + "_Taken", true);
            SalvarProgressoSeguro();
        }
    }

    IEnumerator ShowMissingRunesWarning() 
    { 
        mostrandoErro = true;

        if (runesUIPanel) runesUIPanel.SetActive(false); 
        if (missingRunesText) missingRunesText.SetActive(true); 
        
        yield return new WaitForSeconds(2f); 
        
        if (missingRunesText) missingRunesText.SetActive(false); 

        mostrandoErro = false;

        if (estaOlhando && runesUIPanel && !alreadyTaken)
            runesUIPanel.SetActive(true);
    }

    private void SalvarProgressoSeguro()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(false);
    }
}