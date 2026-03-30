using UnityEngine;

public class DevTools : MonoBehaviour
{
    [Header("Drag Menu Panel Here")]
    public GameObject menuPanel;

    [Header("Altar Reference (Optional)")]
    public WeaponAltar altarScript; 

    [Header("--- DEBUG STARTUP (CHAVES) ---")]
    [Tooltip("Marque essa caixa para já nascer com as chaves no bolso ao dar o Play.")]
    public bool comecarComTodasAsChaves = false;
    
    [Tooltip("Adicione aqui os IDs exatos das suas chaves (Ex: Chave_Porao)")]
    public string[] chavesParaDesbloquear;

    private bool mouseWasVisible;
    private CursorLockMode prevMouseMode;

    void Start()
    {
        if(menuPanel) menuPanel.SetActive(false);

        // A MÁGICA TÁ AQUI: Se a caixa tiver marcada no Unity, ele injeta as chaves no segundo que o jogo abre.
        if (comecarComTodasAsChaves && chavesParaDesbloquear != null)
        {
            foreach (string idChave in chavesParaDesbloquear)
            {
                if (!string.IsNullOrEmpty(idChave))
                {
                    KeySystem.AdicionarChave(idChave);
                }
            }
            Debug.Log("Dev: Jogador iniciou com as chaves injetadas pelo DevTools.");
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F3))
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        bool currentState = menuPanel.activeSelf;
        menuPanel.SetActive(!currentState);

        if (!currentState) 
        {
            prevMouseMode = Cursor.lockState;
            mouseWasVisible = Cursor.visible;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0; 
        }
        else 
        {
            Cursor.lockState = prevMouseMode;
            Cursor.visible = mouseWasVisible;
            Time.timeScale = 1; 
        }
    }

    // --- CHEATS DO MENU ---

    public void CheatGetGlock()
    {
        UnlockWeapon(0); 
        Debug.Log("Dev: Glock added.");
    }

    public void CheatGetCrowbar()
    {
        UnlockWeapon(1); 
        Debug.Log("Dev: Crowbar added.");
    }

    void UnlockWeapon(int id)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReceberItem(id); 
        }
        else
        {
            if (id < EstadoGlobal.armasDesbloqueadas.Length)
                EstadoGlobal.armasDesbloqueadas[id] = true;
        }
    }

    public void CheatAddRune(string runeName)
    {
        if(InventarioRunas.Instance != null)
        {
            InventarioRunas.Instance.ColetarRunaPeloNome(runeName);
            Debug.Log("Dev: Rune " + runeName + " added.");
        }
    }

    public void CheatResetAltarVisual()
    {
        if(altarScript != null)
        {
            altarScript.SpawnWeaponOnAltar(); 
            Debug.Log("Dev: Weapon spawned VISUALLY on altar.");
        }
    }
}