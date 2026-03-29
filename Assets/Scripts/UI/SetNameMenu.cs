using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SetNameMenu : MonoBehaviour
{
    void Start()
    {
        string path = Path.Combine(Application.persistentDataPath, "playerProfile.dat");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "Player");
        }
        StartCoroutine(LoadNextScene("Boot"));
    }

    private IEnumerator LoadNextScene(string sceneName)
    {
        yield return null; // wait a frame for iOS
        SceneManager.LoadScene(sceneName);
    }
}