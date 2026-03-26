using UnityEngine;
using UnityEngine.UI;

public class MultiplayerMenu : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject friendsWorldButtonPrefab;

    [Header("UI Elements")]
    public Button playButton;
    public Button cancelButton;
    public Transform friendWorldsContainer;

    void Awake()
    {
        cancelButton.onClick.AddListener(Cancel);
        playButton.onClick.AddListener(Play);
    }

    public void Cancel()
    {
        Destroy(gameObject);
    }

    public void Play()
    {
        LoadingMenu.Create(LoadingMenuType.ConnectServer);
    }
}