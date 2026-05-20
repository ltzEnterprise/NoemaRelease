using UnityEngine;
using System.Collections;

public class MagicalPhoto : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("ID único desta foto. Ex: Foto_Castelo_01. NÃO deixe repetido entre fotos diferentes.")]
    public string uniqueID = "Foto_Magica_01";

    [Header("--- HORÁRIO NECESSÁRIO ---")]
    public DayNightCycle.TimeState horarioNecessario = DayNightCycle.TimeState.InitialDay;

    [Header("--- PHOTO ANIMATION (HAND) ---")]
    public Vector3 restingPosition;
    public Vector3 restingRotation;
    public Vector3 restingScale = Vector3.one;
    
    public Vector3 aimingPosition;
    public Vector3 aimingRotation;
    public Vector3 aimingScale = Vector3.one;
    
    public float animationSpeed = 10f;

    [Header("--- ALIGNMENT SETTINGS ---")]
    public Transform idealPoint;
    public float maxDistance = 2.5f;
    public float maxAngle = 18.0f;

    [Header("--- THE MAGIC ---")]
    public GameObject objectToReveal;
    public bool hideInsteadOfReveal = false; 
    public AudioSource audioSource;
    public AudioClip revealSound;
    public AudioClip erroHorarioSound;

    [Header("--- CONSUMIR ITEM DO INVENTÁRIO ---")]
    public bool consumirFotoAoUsar = true;
    public int idDaFotoNoInventario = 0;

    [Header("--- UI ---")]
    public GameObject textoDicaMagica;

    private bool isAiming = false;
    private bool alreadyUsed = false;
    private bool inicializado = false;

    void Start()
    {
        if (textoDicaMagica)
            textoDicaMagica.SetActive(false);

        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );
        }

        bool estaFotoJaFoiUsada = false;

        if (PersistenciaManager.Instance != null)
            estaFotoJaFoiUsada = PersistenciaManager.Instance.ObterEstado(ChaveFotoUsada(), false);

        if (estaFotoJaFoiUsada)
        {
            alreadyUsed = true;

            if (objectToReveal)
                objectToReveal.SetActive(hideInsteadOfReveal ? false : true);

            if (textoDicaMagica)
                textoDicaMagica.SetActive(false);

            gameObject.SetActive(false);
            yield break;
        }

        // Se esta foto específica ainda NÃO foi usada, o objeto fica no estado inicial.
        // Não lê mais estado global de objectToReveal.name nem chave separada do objeto.
        // Isso impede uma foto de afetar outra.
        if (objectToReveal)
            objectToReveal.SetActive(hideInsteadOfReveal ? true : false);

        inicializado = true;
    }

    void Update()
    {
        if (!inicializado) return;
        if (alreadyUsed) return;

        if (Input.GetMouseButtonDown(1))
            isAiming = true;

        if (Input.GetMouseButtonUp(1))
            isAiming = false;

        Vector3 targetPos = isAiming ? aimingPosition : restingPosition;
        Quaternion targetRot = Quaternion.Euler(isAiming ? aimingRotation : restingRotation);
        Vector3 targetScale = isAiming ? aimingScale : restingScale;

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * animationSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * animationSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);

        bool noPontoCerto = false;
        bool horarioCerto = DayNightCycle.Instance != null && DayNightCycle.Instance.currentState == horarioNecessario;
        
        if (isAiming && idealPoint != null && Camera.main != null && horarioCerto)
        {
            float dist = Vector3.Distance(Camera.main.transform.position, idealPoint.position);
            float angle = Quaternion.Angle(Camera.main.transform.rotation, idealPoint.rotation);
            noPontoCerto = dist <= maxDistance && angle <= maxAngle;
        }

        if (textoDicaMagica)
            textoDicaMagica.SetActive(noPontoCerto);

        if (isAiming && Input.GetMouseButtonDown(0))
            TryRevealObject();
    }

    void TryRevealObject()
    {
        bool horarioCerto = DayNightCycle.Instance != null && DayNightCycle.Instance.currentState == horarioNecessario;

        if (!horarioCerto) 
        {
            if (audioSource && erroHorarioSound)
                audioSource.PlayOneShot(erroHorarioSound);

            return;
        }

        if (idealPoint == null || Camera.main == null)
            return;

        float dist = Vector3.Distance(Camera.main.transform.position, idealPoint.position);
        float angle = Quaternion.Angle(Camera.main.transform.rotation, idealPoint.rotation);

        if (dist <= maxDistance && angle <= maxAngle) 
        {
            if (textoDicaMagica)
                textoDicaMagica.SetActive(false); 

            StartCoroutine(SequenciaVitoria());
        }
    }

    IEnumerator SequenciaVitoria()
    {
        if (alreadyUsed) yield break;

        alreadyUsed = true;
        inicializado = false;
        isAiming = false;
        
        if (objectToReveal)
            objectToReveal.SetActive(hideInsteadOfReveal ? false : true);

        if (audioSource && revealSound)
            audioSource.PlayOneShot(revealSound);

        RegistrarUsoDestaFotoNoSave();

        if (consumirFotoAoUsar)
            RemoverSomenteEstaFotoDoInventario();

        float t = 0f;
        Vector3 currentPos = transform.localPosition;
        Vector3 currentScale = transform.localScale;
        
        while (t < 1f)
        {
            t += Time.deltaTime * 6f; 
            transform.localPosition = Vector3.Lerp(currentPos, currentPos + (Vector3.down * 2f), t);
            transform.localScale = Vector3.Lerp(currentScale, Vector3.zero, t); 
            yield return null;
        }

        SalvarProgressoSeguro();

        gameObject.SetActive(false);
    }

    private void RegistrarUsoDestaFotoNoSave()
    {
        if (PersistenciaManager.Instance == null)
            return;

        PersistenciaManager.Instance.RegistrarEstado(ChaveFotoUsada(), true);
    }

    private void RemoverSomenteEstaFotoDoInventario()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ConsumirItem(idDaFotoNoInventario);
        }
        else
        {
            RegistrarItemBloqueadoNoSave(idDaFotoNoInventario);
        }
    }

    private void RegistrarItemBloqueadoNoSave(int id)
    {
        if (PersistenciaManager.Instance == null)
            return;

        if (id < 0)
            return;

        if (SistemaGlobal.Instance != null &&
            SistemaGlobal.Instance.slotFoiDefinido &&
            SistemaGlobal.Instance.slotAtual > 0)
        {
            string prefixo = "Slot_" + SistemaGlobal.Instance.slotAtual;

            PersistenciaManager.Instance.RegistrarEstado(prefixo + "_InvUnlocked_" + id, false);

            int itemSelecionado = PersistenciaManager.Instance.ObterInt(prefixo + "_Inv_ItemSelected", -1);

            if (itemSelecionado == id)
                PersistenciaManager.Instance.SalvarInt(prefixo + "_Inv_ItemSelected", -1);
        }

        PersistenciaManager.Instance.RegistrarEstado("InvUnlocked_" + id, false);

        int itemSelecionadoGlobal = PersistenciaManager.Instance.ObterInt("Inv_ItemSelected", -1);

        if (itemSelecionadoGlobal == id)
            PersistenciaManager.Instance.SalvarInt("Inv_ItemSelected", -1);
    }

    private string ChaveFotoUsada()
    {
        // A chave agora usa:
        // - cena
        // - caminho do objeto na hierarquia
        // - uniqueID
        // - ID do item no inventário
        //
        // Isso evita uma foto marcar outra como usada, mesmo se você esquecer uniqueID igual.
        string cena = gameObject.scene.IsValid() ? gameObject.scene.name : "CenaSemNome";
        string caminho = ObterCaminhoHierarquia(transform);
        string id = string.IsNullOrEmpty(uniqueID) ? "FotoSemID" : uniqueID.Trim();

        return "MagicalPhoto_" +
               NormalizarTexto(cena) + "_" +
               NormalizarTexto(caminho) + "_" +
               NormalizarTexto(id) + "_Item_" +
               idDaFotoNoInventario + "_Usada";
    }

    private string ObterCaminhoHierarquia(Transform t)
    {
        if (t == null)
            return "ObjetoNulo";

        string caminho = t.name;
        Transform atual = t.parent;

        while (atual != null)
        {
            caminho = atual.name + "/" + caminho;
            atual = atual.parent;
        }

        return caminho;
    }

    private string NormalizarTexto(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return "vazio";

        return texto
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace("(", "_")
            .Replace(")", "_")
            .Replace("-", "_")
            .Replace(".", "_")
            .Replace(":", "_");
    }

    private void SalvarProgressoSeguro()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }
}