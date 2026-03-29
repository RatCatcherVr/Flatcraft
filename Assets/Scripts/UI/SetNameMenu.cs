using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SetNameMenu : MonoBehaviour
{
    public InputField nameField;

    public void DoneButton()
    {
        SetName(nameField.text);
        SceneManager.LoadScene("Boot");
    }

    private void SetName(string name)
    {
        // Use Path.Combine to handle platform-specific separators
        string testingNamePath = Path.Combine(Application.persistentDataPath, "playerProfile.dat");
        File.WriteAllText(testingNamePath, name);

        // Optional: force flush to make sure iOS saves it immediately
        // File.WriteAllText does this normally, but iOS can be weird
    }
}