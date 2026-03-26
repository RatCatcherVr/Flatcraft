using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowOnPC : MonoBehaviour
{
    void Start()
    {
        // Explicitly use UnityEngine.Application to avoid ambiguity
        bool isPC = UnityEngine.Application.platform == RuntimePlatform.WindowsPlayer
                    || UnityEngine.Application.platform == RuntimePlatform.WindowsEditor
                    || UnityEngine.Application.platform == RuntimePlatform.OSXPlayer
                    || UnityEngine.Application.platform == RuntimePlatform.LinuxPlayer;

        gameObject.SetActive(isPC);
    }
}