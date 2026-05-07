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

    private string ChaveAltarUsado
    {
        get { return "WeaponAltar_Weapon_" + weaponIDToUnlock + "_Taken"; }
    }

    private string ChaveAltarAtivado
    {
        get { return "WeaponAltar_Weapon_" + weaponIDToUnlock + "_Activated"; }
    }

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

        bool altarUsado = PersistenciaManager.Instance.ObterEstado(ChaveAltarUsado, false);
        bool altarAtivado = PersistenciaManager.Instance.ObterEstado(ChaveAltarAtivado, false);

        alreadyTaken = altarUsado || altarAtivado;

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

            SalvarAtivacaoDoAltarImediatamente();
            StartPickupProcess();
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(ShowMissingRunesWarning());
        }
    }

    private void SalvarAtivacaoDoAltarImediatamente()
    {
        alreadyTaken = true;

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveAltarAtivado, true);
            PersistenciaManager.Instance.RegistrarEstado(ChaveAltarUsado, true);

            PersistenciaManager.Instance.SalvarTudo(true);
        }

        if (weaponIDToUnlock >= 0 && weaponIDToUnlock < EstadoGlobal.armasDesbloqueadas.Length)
        {
            EstadoGlobal.armasDesbloqueadas[weaponIDToUnlock] = true; 
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
            PersistenciaManager.Instance.RegistrarEstado(ChaveAltarAtivado, true);
            PersistenciaManager.Instance.RegistrarEstado(ChaveAltarUsado, true);

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
        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }
}