using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingPC : MonoBehaviour
{
    [Header("--- POST-PROCESSING DA CENA ---")]
    [Tooltip("Coloque aqui o SEU Volume que deixa a cena bonita")]
    public GameObject volumeDessaCena;
    
    [Tooltip("(Opcional) Se tiver algum Volume Global cagando a cena, joga ele aqui pra desativar")]
    public GameObject volumeGlobalPraDesativar;

    [Header("--- LIMITES DO MAPA ---")]
    public Transform playerTransform;
    public Transform bedTeleportPoint; 
    public float maxDistance = 500f;

    [Header("--- CUTSCENE DE ENCERRAMENTO ---")]
    public Transform cameraPoint1;
    public Transform cameraPoint2; 
    public float timeToPoint1 = 2f;
    public float timeToPoint2 = 3f;

    [Header("--- UI E CRÉDITOS ---")]
    public GameObject interactText; 
    public Image blackBackground; 
    public CanvasGroup creditNameCanvas; 
    public float blackScreenFadeTime = 2f;
    public float nameFadeTime = 2f;
    public float nameHoldTime = 4f;

    [Header("--- NAVEGAÇÃO ---")]
    public string menuSceneName = "MenuPrincipal";

    private bool inCutscene = false;
    private bool volumeGlobalEstavaAtivo = false;

    void Start()
    {
        // 1. Desliga a neblina base da Unity por garantia
        RenderSettings.fog = false;

        // 2. Desliga o volume global (se existir) e liga o seu volume perfeito
        if (volumeGlobalPraDesativar != null)
        {
            volumeGlobalEstavaAtivo = volumeGlobalPraDesativar.activeSelf;
            volumeGlobalPraDesativar.SetActive(false);
        }

        if (volumeDessaCena != null)
        {
            volumeDessaCena.SetActive(true);
        }

        if (interactText != null) interactText.SetActive(false);
        
        if (blackBackground != null)
        {
            Color c = blackBackground.color;
            c.a = 0f;
            blackBackground.color = c;
            blackBackground.gameObject.SetActive(false);
        }

        if (creditNameCanvas != null)
        {
            creditNameCanvas.alpha = 0f;
            creditNameCanvas.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Quando a cena acabar, devolve as coisas pro lugar pra não quebrar o Menu/Jogo
        if (volumeGlobalPraDesativar != null && volumeGlobalEstavaAtivo)
        {
            volumeGlobalPraDesativar.SetActive(true);
        }
    }

    void Update()
    {
        if (inCutscene || playerTransform == null) return;

        if (Vector3.Distance(playerTransform.position, transform.position) > maxDistance)
        {
            TeleportBack();
        }
    }

    void TeleportBack()
    {
        if (bedTeleportPoint == null) return;

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerTransform.position = bedTeleportPoint.position;
        playerTransform.rotation = bedTeleportPoint.rotation;

        if (cc != null) cc.enabled = true;
    }

    public void AoOlhar()
    {
        if (inCutscene) return;
        if (interactText != null) interactText.SetActive(true);
    }

    public void AoSair()
    {
        if (interactText != null) interactText.SetActive(false);
    }

    public void Interagir()
    {
        if (inCutscene) return;
        AoSair(); 
        StartCoroutine(EndingCutsceneRoutine());
    }

    IEnumerator EndingCutsceneRoutine()
    {
        inCutscene = true;

        if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = true;
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        Camera cam = Camera.main;
        Vector3 initialPos = cam.transform.position;
        Quaternion initialRot = cam.transform.rotation;

        float t = 0f;
        while (t < timeToPoint1)
        {
            t += Time.deltaTime;
            float smoothT = Mathf.SmoothStep(0f, 1f, t / timeToPoint1);
            cam.transform.position = Vector3.Lerp(initialPos, cameraPoint1.position, smoothT);
            cam.transform.rotation = Quaternion.Slerp(initialRot, cameraPoint1.rotation, smoothT);
            yield return null;
        }

        t = 0f;
        while (t < timeToPoint2)
        {
            t += Time.deltaTime;
            float smoothT = Mathf.SmoothStep(0f, 1f, t / timeToPoint2);
            cam.transform.position = Vector3.Lerp(cameraPoint1.position, cameraPoint2.position, smoothT);
            cam.transform.rotation = Quaternion.Slerp(cameraPoint1.rotation, cameraPoint2.rotation, smoothT);
            yield return null;
        }

        if (blackBackground != null)
        {
            blackBackground.gameObject.SetActive(true);
            t = 0f;
            while (t < blackScreenFadeTime)
            {
                t += Time.deltaTime;
                Color c = blackBackground.color;
                c.a = Mathf.Lerp(0f, 1f, t / blackScreenFadeTime);
                blackBackground.color = c;
                yield return null;
            }
        }

        yield return new WaitForSeconds(1f);

        if (creditNameCanvas != null)
        {
            creditNameCanvas.gameObject.SetActive(true);
            t = 0f;
            while (t < nameFadeTime)
            {
                t += Time.deltaTime;
                creditNameCanvas.alpha = Mathf.Lerp(0f, 1f, t / nameFadeTime);
                yield return null;
            }

            yield return new WaitForSeconds(nameHoldTime);

            t = 0f;
            while (t < nameFadeTime)
            {
                t += Time.deltaTime;
                creditNameCanvas.alpha = Mathf.Lerp(1f, 0f, t / nameFadeTime);
                yield return null;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(menuSceneName);
    }
}