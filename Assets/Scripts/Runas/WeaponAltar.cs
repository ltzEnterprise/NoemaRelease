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

    [Tooltip("Aqui deve ser o OBJETO FÍSICO da arma na mesa/altar, com ItemPickup. Não é a arma da mão.")]
    public GameObject visualWeapon;

    public GameObject missingRunesText;
    public GameObject highlightLight;

    private bool isWeaponAvailable = false;
    private bool altarActivated = false;
    private bool weaponPlacedOnAltar = false;

    private bool estaOlhando = false;
    private bool mostrandoErro = false;

    private string ChaveAltarAtivado
    {
        get { return "WeaponAltar_Weapon_" + weaponIDToUnlock + "_Activated"; }
    }

    private string ChaveArmaNoAltar
    {
        get { return "WeaponAltar_Weapon_" + weaponIDToUnlock + "_WeaponOnAltar"; }
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
            PersistenciaManager.Instance.DadosProntosParaUso &&
            !PersistenciaManager.Instance.EstaCarregando
        );

        bool altarAtivado = PersistenciaManager.Instance.ObterEstado(ChaveAltarAtivado, false);
        bool armaNoAltar = PersistenciaManager.Instance.ObterEstado(ChaveArmaNoAltar, false);

        altarActivated = altarAtivado;
        weaponPlacedOnAltar = armaNoAltar || altarAtivado;
        isWeaponAvailable = weaponPlacedOnAltar;

        if (weaponPlacedOnAltar)
        {
            ColocarArmaNoAltarVisualmente();

            if (runesUIPanel) runesUIPanel.SetActive(false);
            if (missingRunesText) missingRunesText.SetActive(false);

            Debug.Log("[WeaponAltar] Estado carregado: arma física já estava liberada no altar/mesa. WeaponID=" + weaponIDToUnlock);
        }
    }

    public void SpawnWeaponOnAltar()
    {
        isWeaponAvailable = true;
        weaponPlacedOnAltar = true;

        ColocarArmaNoAltarVisualmente();
    }

    private void ColocarArmaNoAltarVisualmente()
    {
        weaponPlacedOnAltar = true;
        isWeaponAvailable = true;

        if (visualWeapon)
            visualWeapon.SetActive(true);

        if (highlightLight)
            highlightLight.SetActive(true);
    }

    public void AoOlhar()
    {
        if (altarActivated) return;

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
        if (altarActivated) return;

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

            SalvarArmaColocadaNoAltar();
            StartAltarProcess();
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(ShowMissingRunesWarning());
        }
    }

    private void SalvarArmaColocadaNoAltar()
    {
        altarActivated = true;
        weaponPlacedOnAltar = true;
        isWeaponAvailable = true;

        ColocarArmaNoAltarVisualmente();

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveAltarAtivado, true);
            PersistenciaManager.Instance.RegistrarEstado(ChaveArmaNoAltar, true);
            PersistenciaManager.Instance.SalvarTudo(true);
        }

        Debug.Log("[WeaponAltar] Arma física colocada no altar/mesa e salva. WeaponID=" + weaponIDToUnlock);
    }

    void StartAltarProcess()
    {
        altarActivated = true;
        weaponPlacedOnAltar = true;

        if (runesUIPanel) runesUIPanel.SetActive(false);
        if (missingRunesText) missingRunesText.SetActive(false);

        ColocarArmaNoAltarVisualmente();

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
        Debug.Log("[WeaponAltar] FinalizeActivation: altar finalizado, arma continua física para ser pega com ItemPickup.");

        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.ChangeTo(DayNightCycle.TimeState.Night);

        altarActivated = true;
        weaponPlacedOnAltar = true;
        isWeaponAvailable = true;

        ColocarArmaNoAltarVisualmente();

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveAltarAtivado, true);
            PersistenciaManager.Instance.RegistrarEstado(ChaveArmaNoAltar, true);
            PersistenciaManager.Instance.SalvarTudo(true);
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

        if (estaOlhando && runesUIPanel && !altarActivated)
            runesUIPanel.SetActive(true);
    }
}