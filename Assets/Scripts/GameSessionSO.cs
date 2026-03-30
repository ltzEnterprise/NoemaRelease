using UnityEngine;


public class GameSessionSO : ScriptableObject
{
   
    public Vector3 ultimaPosicao3D; // Salva onde o player estava no 3D
    public bool isRetornandoDo2D;   // Flag para saber se devemos reposicionar o player
    public bool temChave;           // Se pegou a chave no 2D

    // Reseta os dados ao iniciar o jogo (opcional, para testes)
    public void ResetarSessao()
    {
        ultimaPosicao3D = Vector3.zero;
        isRetornandoDo2D = false;
        temChave = false;
    }
}