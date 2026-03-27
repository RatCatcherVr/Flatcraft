using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SingleplayerMenu : MonoBehaviour
{
    public GameObject deleteWorldMenuPrefab;
    public GameObject createWorldMenuPrefab;

    [Space]
    [Space]

    public Transform list;
    public int selectedWorld = -1;
    public GameObject worldPrefab;
    public List<World> worlds = new List<World>();
    public Button playButton;
    public Button deleteButton;

    private void Start()
    {
        LoadWorlds();
    }

    private void Update()
    {
        playButton.interactable = selectedWorld != -1;
        deleteButton.interactable = selectedWorld != -1;
    }

    public void LoadWorlds()
    {
        worlds = GetWorlds();
        worlds.Sort((a, b) => b.lastModifiedUTC.CompareTo(a.lastModifiedUTC));

        foreach (Transform child in list) Destroy(child.gameObject);

        for (int i = 0; i < worlds.Count; i++)
        {
            int index = i;
            GameObject obj = Instantiate(worldPrefab, list, false);

            UnityEngine.UI.Text worldNameText = obj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (worldNameText != null) worldNameText.text = worlds[i].name;

            Button btn = obj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    selectedWorld = index;
                });
            }
        }
    }

    public static List<World> GetWorlds()
    {
        return World.LoadWorlds();
    }

    public void Cancel()
    {
        Destroy(gameObject);
    }

    public void Play()
    {
        Sound.PlayLocal(new Location(), "menu/click", 0, SoundType.Menu, 1f, 100000f, false);
        if (selectedWorld < 0 || selectedWorld >= worlds.Count) return;

        WorldManager.world = worlds[selectedWorld];
        LoadingMenu.Create(LoadingMenuType.LoadWorld);
        MultiplayerManager.HostGameAsync();
    }

    public void Delete()
    {
        if (selectedWorld < 0 || selectedWorld >= worlds.Count) return;

        DeleteWorldMenu.SelectedWorld = worlds[selectedWorld];
        Instantiate(deleteWorldMenuPrefab);
        Destroy(gameObject);
    }

    public void Create()
    {
        Instantiate(createWorldMenuPrefab);
        Destroy(gameObject);
    }
}