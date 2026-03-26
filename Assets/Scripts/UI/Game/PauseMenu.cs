using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PauseMenu : MonoBehaviour
{
    public static bool active;
    public GameObject optionsMenuPrefab;

    public GameObject toggleMenuButton; // drag your button here

    private void Start()
    {
        active = false;
        SetMenuActive(false);
    }

    public void ToggleMenu()
    {
        if (!active && !PlayerInteraction.CanInteractWithWorld()) return;

        SetMenuActive(!active);
    }

    public void Options()
    {
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
        cg.alpha = active ? 1 : 0;
        cg.interactable = active;
        cg.blocksRaycasts = active;

        // 👇 THIS is the important part
        if (toggleMenuButton != null)
            toggleMenuButton.SetActive(!active);
    }

    public void BackToMainMenu()
    {
        ((MultiplayerManager)NetworkManager.singleton).StopConnection();
    }
}