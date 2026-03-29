using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine;

public class Boot : MonoBehaviour
{
    private const string TestWorldName = "Test";

    public bool autoloadTestWorld;

    void Start()
    {
        // Option to immediately load test world in editor
        if (CreateNameCheck()) return;
        if (TryLoadTestWorld()) return;

        SceneManager.LoadScene("MainMenu"); // Load main menu first, so it is active
        SceneManager.LoadScene("Intro", LoadSceneMode.Additive);
    }

    private bool CreateNameCheck()
    {
        // Use Path.Combine for cross-platform paths
        string testingNamePath = Path.Combine(Application.persistentDataPath, "playerProfile.dat");

        if (!File.Exists(testingNamePath))
        {
            // First launch: auto-create name and skip SetName scene
            File.WriteAllText(testingNamePath, "Player");
            return false; // Return false so we continue loading MainMenu
        }

        SettingsManager.PlayerName = File.ReadAllText(testingNamePath);
        return false;
    }

    private bool TryLoadTestWorld()
    {
        if (autoloadTestWorld && Application.isEditor)
        {
            if (World.WorldExists(TestWorldName))
                WorldManager.world = World.LoadWorld(TestWorldName);
            else
                WorldManager.world = new World(TestWorldName, (new System.Random()).Next());

            MultiplayerManager.HostGameAsync();
            return true;
        }

        return false;
    }
}