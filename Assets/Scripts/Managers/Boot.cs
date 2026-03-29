using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

public class Boot : MonoBehaviour
{
    void Start()
    {
        string profilePath = Path.Combine(Application.persistentDataPath, "playerProfile.dat");
        if (!File.Exists(profilePath))
        {
            File.WriteAllText(profilePath, "Player");
        }

        StartCoroutine(LoadMainMenu());
    }

    private IEnumerator LoadMainMenu()
    {
        yield return null; // wait a frame for iOS
        SceneManager.LoadScene("MainMenu");
        SceneManager.LoadScene("Intro", LoadSceneMode.Additive);
    }
}