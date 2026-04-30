using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ScanSaveUsage
{
    static string[] termos = new string[]
    {
        "SalvarTudo",
        "RegistrarEstado",
        "ObterEstado",
        "SalvarInt",
        "SalvarFloat",
        "SalvarString",
        "ObterInt",
        "ObterFloat",
        "ObterString",
        "PlayerPrefs",
        "LoadScene"
    };

    // 🔥 BLACKLIST — scripts que NÃO queremos ver
static HashSet<string> blacklist = new HashSet<string>()
{
    "SistemaGlobal.cs",
    "GameManager.cs",
    "PersistenciaManager.cs",
    "SaveSlotManager.cs",
    "SaveableItem.cs",
    "EstadoGlobal.cs",
    "InterfaceManager.cs",
    "InventoryManager.cs",
    "InventarioRunas.cs",
    "KeyManager.cs",

    "QuizDoor.cs",
    "AudioPuzzleDoor.cs",
    "ChildPuzzleDoor.cs",
    "Keypad.cs",
    "ReadableDocument.cs",
    "CastleDoor.cs",

    "ItemPickup.cs",
    "TreasureChest.cs",
    "DoorWithKeyTeleport.cs",
    "OneSidedDoor.cs",
    "InvestigationHouseDoor.cs",
    "InvestigationHouseExit.cs",
    "PilarReceptor.cs",

    "WeaponAltar.cs",
    "MagicalPhoto.cs",
    "TVInterativa.cs",
    "TurntableSystem.cs",

    "FuseBoxController.cs",
    "FuseSwitch.cs",
    "ManagerMundo2D.cs",
    "LevelGoal.cs",

    "WindowJump.cs",
    "WoodenBarricade.cs",

    "ComputerController.cs",
"BedController.cs",
"EndingPC.cs",
"FPS_Flycam.cs",
"FPS_Master.cs",
"RealityCamera.cs",
"ReceiverDoor.cs",
"SpikeTrap.cs",
"FinalPC.cs",
"DevToolsEditor.cs",
"ScanSaveUsage.cs",
"CaptainLeverPuzzle.cs",
"DayNightCycle.cs",
"LanguageManager.cs",
"PillarManager.cs",
"RotationPuzzleManager.cs",
"SettingsManager.cs",
"SlidingPuzzleManager.cs",
"SM_Menu.cs",
"SM_SaveDropdown.cs",
"SM_SaveOptionList.cs",
"SM_SaveSlider.cs",
"SM_SaveToggle.cs"
};

    [MenuItem("Tools/Scan Save System Limpo")]
    public static void Scan()
    {
        string[] arquivos = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        Debug.Log("=== SCAN LIMPO (SEM SCRIPTS JÁ REVISADOS) ===");

        foreach (string path in arquivos)
        {
            string nomeArquivo = Path.GetFileName(path);

            // 🔥 ignora blacklist
            if (blacklist.Contains(nomeArquivo))
                continue;

            string codigo = File.ReadAllText(path);
            List<string> encontrados = new List<string>();

            foreach (string termo in termos)
            {
                if (codigo.Contains(termo))
                    encontrados.Add(termo);
            }

            if (encontrados.Count > 0)
            {
                string relativePath = "Assets" + path.Replace(Application.dataPath, "").Replace("\\", "/");
                Debug.LogWarning($"[SCRIPT NOVO] {relativePath} | Usa: {string.Join(", ", encontrados)}");
            }
        }

        Debug.Log("=== SCAN FINALIZADO ===");
    }
}