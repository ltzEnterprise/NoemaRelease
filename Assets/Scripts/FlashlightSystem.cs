using UnityEngine;
using TMPro; // Se usar TextMeshPro

public class FlashlightSystem : MonoBehaviour
{
    [Header("Objetos de Luz")]
    public GameObject luzNormalObj; // Arraste a Spot Light Branca
    public GameObject luzUVObj;     // Arraste a Spot Light Roxa

    [Header("UI")]
    public GameObject avisoUI;      // Arraste o texto "Aperte F para mudar modo" do Canvas

    private enum ModoLanterna { Normal, UV, Desligada }
    private ModoLanterna modoAtual = ModoLanterna.Normal;

    // Roda toda vez que o Inventário ativa este objeto (Saca a lanterna)
    void OnEnable()
    {
        if(avisoUI != null) avisoUI.SetActive(true);
        modoAtual = ModoLanterna.Normal; // Sempre começa com a luz normal ao sacar
        AtualizarLuzes();
    }

    // Roda quando guarda a lanterna
    void OnDisable()
    {
        if(avisoUI != null) avisoUI.SetActive(false);
    }

    void Update()
    {
        // Se este objeto está ativo, ele ouve o F para trocar de modo
        if (Input.GetKeyDown(KeyCode.F))
        {
            TrocarModo();
        }
    }

    void TrocarModo()
    {
        switch (modoAtual)
        {
            case ModoLanterna.Normal:
                modoAtual = ModoLanterna.UV;
                break;
            case ModoLanterna.UV:
                modoAtual = ModoLanterna.Desligada;
                break;
            case ModoLanterna.Desligada:
                modoAtual = ModoLanterna.Normal;
                break;
        }
        AtualizarLuzes();
    }

    void AtualizarLuzes()
    {
        luzNormalObj.SetActive(false);
        luzUVObj.SetActive(false);

        if (modoAtual == ModoLanterna.Normal) luzNormalObj.SetActive(true);
        else if (modoAtual == ModoLanterna.UV) luzUVObj.SetActive(true);
    }
}