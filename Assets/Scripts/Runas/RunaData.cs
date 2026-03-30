using UnityEngine;

[CreateAssetMenu(fileName = "NovaRuna", menuName = "Sistema de Runas/Nova Runa")]
public class RunaData : ScriptableObject
{
    public string nomeDaRuna;
    public Sprite icone; // Opcional, caso queira mostrar na UI depois
}