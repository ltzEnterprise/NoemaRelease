using UnityEngine;
using System.Collections.Generic;

public class AmbientSkyObject : MonoBehaviour
{
    public static List<AmbientSkyObject> todosObjetos = new List<AmbientSkyObject>();

    public enum TipoFoto { Decoracao, ChaveFinal, LinkPilar }

    [Header("--- TIPO DE FOTO ---")]
    [Tooltip("Escolha 'ChaveFinal' para a chave gigante")]
    public TipoFoto tipoDeInteracao = TipoFoto.Decoracao;

    [Header("Apenas se for Pilar")]
    public int idLinkPilar = 0; 
    
    [Header("Efeitos")]
    public GameObject efeitoAoSumir;
    public bool sumirAposFoto = true;

    void OnEnable() { if (!todosObjetos.Contains(this)) todosObjetos.Add(this); }
    void OnDisable() { if (todosObjetos.Contains(this)) todosObjetos.Remove(this); }

    public void Sumir()
    {
        if (efeitoAoSumir != null)
            Instantiate(efeitoAoSumir, transform.position, Quaternion.identity);

        if (sumirAposFoto) gameObject.SetActive(false);
    }
}