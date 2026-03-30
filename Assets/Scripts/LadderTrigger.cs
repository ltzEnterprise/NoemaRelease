using UnityEngine;

public class LadderTrigger : MonoBehaviour
{
    public LadderSystem sistemaDaEscada;
    public bool ehParteDeCima; // Marque TRUE no colisor de cima, FALSE no de baixo

    // Repassa os eventos do Raycast para o cérebro (Sistema)
    public void AoOlhar() => sistemaDaEscada.NotificarOlhar(ehParteDeCima);
    public void AoSair() => sistemaDaEscada.NotificarSair();
    public void Interagir() => sistemaDaEscada.NotificarInteracao(ehParteDeCima);
}