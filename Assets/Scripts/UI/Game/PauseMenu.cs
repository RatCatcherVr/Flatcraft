using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PauseMenu : MonoBehaviour
{
    public static bool active;
    public GameObject optionsMenuPrefab;

    public GameObject toggleMenuButton; // drag your mobile button here

    private void Start()
    {
        active = false;
        SetMenuActive(false);

        // Show toggle button only on mobile
        if (toggleMenuButton != null)
        {
            if (Application.isMobilePlatform)
                toggleMenuButton.SetActive(true);
            else
                toggleMenuButton.SetActive(false);
        }
    }

    private void Update()
    {
        // ESC opens menu on PC/Mac
        if (!Application.isMobilePlatform && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (!active && !PlayerInteraction.CanInteractWithWorld()) return;
        SetMenuActive(!active);
    }

    public void Options()
    {
        if (optionsMenuPrefab != null)
            Instantiate(optionsMenuPrefab);
    }

    public void EnterMenu()
    {
        SetMenuActive(true);
    }

    public void BackToGame()
    {
        SetMenuActive(false);
    }

    public void SetMenuActive(bool setActive)
    {
        active = setActive;

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        cg.alpha = active ? 1 : 0;
        cg.interactable = active;
        cg.blocksRaycasts = active;

        // Only toggle mobile button on mobile
        if (toggleMenuButton != null && Application.isMobilePlatform)
        {
            toggleMenuButton.SetActive(!active);
        }
    }

    public void BackToMainMenu()
    {
        if (NetworkManager.singleton != null)
        {
            ((MultiplayerManager)NetworkManager.singleton).StopConnection();
        }
    }
}