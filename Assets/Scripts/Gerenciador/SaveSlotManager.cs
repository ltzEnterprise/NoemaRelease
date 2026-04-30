using UnityEngine;
using System.IO;
using System;

public class SaveSlotManager : MonoBehaviour
{
    public static SaveSlotManager Instance;

    [Header("Referência Obrigatória")]
    [Tooltip("Arraste o seu InterfaceManager aqui para o sistema de slots ler os textos dele.")]
    public InterfaceManager uiManager;

    [Header("--- AJUSTE VISUAL ---")]
    [Tooltip("Quando o botão de apagar vira CONFIRMAR, reduz a fonte esse tanto.")]
    public float reduzirFonteConfirmarApagar = 2f;

    private float[] tamanhosOriginaisBotaoApagar;

    public string DiretorioSaves 
    { 
        get 
        { 
            string dir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        } 
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (uiManager == null)
            uiManager = GetComponent<InterfaceManager>();

        if (!Directory.Exists(DiretorioSaves))
            Directory.CreateDirectory(DiretorioSaves);

        CachearTamanhosOriginaisApagar();
    }

    void Start()
    {
        CachearTamanhosOriginaisApagar();
        ForcarAutoSizeCentral();
        AtualizarTextosSlots();
    }

    void Update()
    {
        if (uiManager == null) return;

        if (uiManager.slotConfirmacao != -1 && Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                BOTAO_CANCELAR_APAGAR();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (uiManager.painelConfirmacaoBackup != null && uiManager.painelConfirmacaoBackup.activeSelf)
            {
                BOTAO_CANCELAR_BACKUP_TELA_PRETA();
            }
        }
    }

    private void CachearTamanhosOriginaisApagar()
    {
        if (uiManager == null) return;
        if (uiManager.textosBotaoApagar == null) return;

        if (tamanhosOriginaisBotaoApagar != null &&
            tamanhosOriginaisBotaoApagar.Length == uiManager.textosBotaoApagar.Length)
            return;

        tamanhosOriginaisBotaoApagar = new float[uiManager.textosBotaoApagar.Length];

        for (int i = 0; i < uiManager.textosBotaoApagar.Length; i++)
        {
            if (uiManager.textosBotaoApagar[i] != null)
                tamanhosOriginaisBotaoApagar[i] = uiManager.textosBotaoApagar[i].fontSize;
        }
    }

    public void BOTAO_JOGAR_SLOT(int numeroDoSlot)
    {
        if (uiManager == null)
        {
            Debug.LogError("[SaveSlotManager] uiManager não foi atribuído.");
            return;
        }

        if (numeroDoSlot <= 0)
        {
            Debug.LogError("[SaveSlotManager] Slot inválido: " + numeroDoSlot);
            return;
        }

        if (uiManager.slotConfirmacao != -1)
        {
            uiManager.slotConfirmacao = -1;
            AtualizarTextosSlots();
        }

        if (SistemaGlobal.Instance == null)
        {
            Debug.LogError("[SaveSlotManager] SistemaGlobal.Instance está nulo.");
            return;
        }

        if (PersistenciaManager.Instance == null)
        {
            Debug.LogError("[SaveSlotManager] PersistenciaManager.Instance está nulo.");
            return;
        }

        SistemaGlobal.Instance.DefinirSlot(numeroDoSlot);

        string caminhoArquivo = Path.Combine(DiretorioSaves, $"Save_Slot_{numeroDoSlot}.json");

        if (File.Exists(caminhoArquivo))
        {
            SistemaGlobal.Instance.sistemaPronto = false;

            PersistenciaManager.Instance.LimparDicionario();
            PersistenciaManager.Instance.CarregarDoDisco(numeroDoSlot);

            EstadoGlobal.CarregarDoSlot(numeroDoSlot);

            SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = true;
            SistemaGlobal.Instance.acabouDeCarregar = true;

            string cenaParaCarregar = PersistenciaManager.Instance.ObterString("Slot_" + numeroDoSlot + "_Cena");

            if (string.IsNullOrEmpty(cenaParaCarregar))
                cenaParaCarregar = SistemaGlobal.Instance.nomeCenaPadrao;

            Debug.Log($"<color=green>[LOAD COM LOADING] Slot {numeroDoSlot} carregando cena {cenaParaCarregar}</color>");

            uiManager.IniciarLoadingParaCena(cenaParaCarregar);
            return;
        }

        SistemaGlobal.Instance.sistemaPronto = false;
        SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = false;
        SistemaGlobal.Instance.acabouDeCarregar = false;

        EstadoGlobal.ResetarTudo();

        PersistenciaManager.Instance.LimparDicionario();
        PersistenciaManager.Instance.IniciarNovoJogo(numeroDoSlot);

        PersistenciaManager.Instance.SalvarString($"Slot_{numeroDoSlot}_Cena", uiManager.nomeDaCenaDoJogo);
        PersistenciaManager.Instance.SalvarTudo(true);

        AtualizarTextosSlots();
        uiManager.IniciarLoadingParaCena(uiManager.nomeDaCenaDoJogo);
    }

    public void BOTAO_APAGAR_SLOT(int numeroDoSlot)
    {
        if (uiManager == null) return;

        string caminho = Path.Combine(DiretorioSaves, $"Save_Slot_{numeroDoSlot}.json");

        if (!File.Exists(caminho)) return;

        if (uiManager.slotConfirmacao == numeroDoSlot)
        {
            if (SistemaGlobal.Instance != null)
                SistemaGlobal.Instance.ApagarSave(numeroDoSlot);
            else
            {
                string backup = caminho + ".bak";
                string temp = caminho + ".tmp";

                if (File.Exists(caminho)) File.Delete(caminho);
                if (File.Exists(backup)) File.Delete(backup);
                if (File.Exists(temp)) File.Delete(temp);
            }

            uiManager.slotConfirmacao = -1;
        }
        else
        {
            uiManager.slotConfirmacao = numeroDoSlot;
        }

        AtualizarTextosSlots();
    }

    public void BOTAO_PREPARAR_BACKUP_SLOT(int numeroDoSlot)
    {
        if (uiManager == null) return;

        uiManager.slotParaRestaurar = numeroDoSlot; 
        uiManager.LigarDesligarPainel(uiManager.painelConfirmacaoBackup, true); 
    }

    public void BOTAO_CONFIRMAR_BACKUP_TELA_PRETA()
    {
        if (uiManager == null) return;

        if (uiManager.slotParaRestaurar != -1 && !uiManager.modoDesenvolvedor)
        {
            string caminho = Path.Combine(DiretorioSaves, $"Save_Slot_{uiManager.slotParaRestaurar}.json");
            string backup = caminho + ".bak";

            if (File.Exists(backup))
            {
                File.Copy(backup, caminho, true);

                if (PersistenciaManager.Instance != null &&
                    SistemaGlobal.Instance != null &&
                    SistemaGlobal.Instance.slotFoiDefinido &&
                    SistemaGlobal.Instance.slotAtual == uiManager.slotParaRestaurar)
                {
                    PersistenciaManager.Instance.LimparDicionario();
                    PersistenciaManager.Instance.CarregarDoDisco(uiManager.slotParaRestaurar);
                    EstadoGlobal.CarregarDoSlot(uiManager.slotParaRestaurar);
                }
            }

            AtualizarTextosSlots(); 
        }

        uiManager.slotParaRestaurar = -1; 
        uiManager.LigarDesligarPainel(uiManager.painelConfirmacaoBackup, false); 
    }

    public void BOTAO_CANCELAR_BACKUP_TELA_PRETA()
    {
        if (uiManager == null) return;

        uiManager.slotParaRestaurar = -1; 
        uiManager.LigarDesligarPainel(uiManager.painelConfirmacaoBackup, false); 
    }

    public void BOTAO_CANCELAR_APAGAR()
    {
        if (uiManager == null) return;

        if (uiManager.slotConfirmacao != -1)
        {
            uiManager.slotConfirmacao = -1;
            AtualizarTextosSlots();
        }
    }

    public void AtualizarTextosSlots()
    {
        if (uiManager == null) return;
        if (uiManager.textosDosSlots == null) return;

        CachearTamanhosOriginaisApagar();

        int lang = 0;

        if (PlayerPrefs.HasKey("Idioma"))
            lang = PlayerPrefs.GetInt("Idioma", 0);

        try
        {
            if (LanguageManager.Instance != null)
                lang = LanguageManager.Instance.currentLanguage;
        }
        catch { }

        string txtNovo = (lang == 0) ? uiManager.textoNovoJogo_PT : uiManager.textoNovoJogo_EN;
        string txtApagarBtn = (lang == 0) ? uiManager.textoApagar_PT : uiManager.textoApagar_EN;
        string txtConfirmarBtn = (lang == 0) ? uiManager.textoConfirmar_PT : uiManager.textoConfirmar_EN;
        string txtApagarSave = (lang == 0) ? uiManager.textoApagarSave_PT : uiManager.textoApagarSave_EN;
        string txtSlot = (lang == 0) ? uiManager.textoSlot_PT : uiManager.textoSlot_EN;

        for (int i = 0; i < uiManager.textosDosSlots.Length; i++)
        {
            if (uiManager.textosDosSlots[i] == null) continue;

            int slotNum = i + 1;
            string caminhoArquivo = Path.Combine(DiretorioSaves, $"Save_Slot_{slotNum}.json");
            bool temSave = File.Exists(caminhoArquivo);

            if (uiManager.textosBotaoApagar != null &&
                i < uiManager.textosBotaoApagar.Length &&
                uiManager.textosBotaoApagar[i] != null)
            {
                bool estaConfirmando = uiManager.slotConfirmacao == slotNum;

                uiManager.textosBotaoApagar[i].text =
                    estaConfirmando ? txtConfirmarBtn : txtApagarBtn;

                if (tamanhosOriginaisBotaoApagar != null &&
                    i < tamanhosOriginaisBotaoApagar.Length &&
                    tamanhosOriginaisBotaoApagar[i] > 0f)
                {
                    uiManager.textosBotaoApagar[i].fontSize =
                        estaConfirmando
                            ? Mathf.Max(1f, tamanhosOriginaisBotaoApagar[i] - reduzirFonteConfirmarApagar)
                            : tamanhosOriginaisBotaoApagar[i];
                }
            }

            string cabecalho = $"<size=40%>{txtSlot} {slotNum}</size>\n";

            if (uiManager.slotConfirmacao == slotNum)
            {
                uiManager.textosDosSlots[i].text = cabecalho + $"<color=red>{txtApagarSave}</color>";
            }
            else if (temSave)
            {
                string data;

                try
                {
                    data = File.GetLastWriteTime(caminhoArquivo).ToString("dd/MM/yyyy HH:mm");
                }
                catch
                {
                    data = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                }

                uiManager.textosDosSlots[i].text = cabecalho + $"<size=50%>{data}</size>";
            }
            else 
            {
                uiManager.textosDosSlots[i].text = cabecalho + txtNovo;
            }
        }
    }

    public void ForcarAutoSizeCentral()
    {
        if (uiManager == null) return;
        if (uiManager.textosDosSlots == null) return;

        foreach (var t in uiManager.textosDosSlots) 
        {
            if (t == null) continue;

            t.enableAutoSizing = true;
            t.fontSizeMin = 10;
            t.fontSizeMax = 60; 
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            t.overflowMode = TMPro.TextOverflowModes.Truncate;
            t.margin = new Vector4(5, 5, 5, 5);
            t.rectTransform.localScale = Vector3.one;
        }
    }
}