using UnityEngine;

[CreateAssetMenu(fileName = "NovoDisco", menuName = "Puzzle/Disco de Vinil")]
public class DadosDoDisco : ScriptableObject
{
    public string nomeDoDisco; // Ex: "Sinfonia da Morte"
    public AudioClip musica;   // O arquivo de áudio
    public bool ehODiscoCorreto; // Marca esse checkbox se esse for o disco que resolve o puzzle
}