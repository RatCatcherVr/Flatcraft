using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SingleplayerMenu : MonoBehaviour
{
    public GameObject deleteWorldMenuPrefab;
    public Transform list;
    public GameObject worldPrefab;
    public Button playButton;
    public Button deleteButton;
    public int selectedWorld = -1;
    public List<World> worlds = new List<World>();

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
        World world = new World();

        string baseName = "World";
        string uniqueName = baseName;
        int counter = 1;
        while (World.WorldExists(uniqueName))
        {
            uniqueName = baseName + counter;
            counter++;
        }
        world.name = uniqueName;
        world.seed = new System.Random().Next();
        world.SaveData();

        WorldManager.world = world;
        MultiplayerManager.HostGameAsync();
        LoadingMenu.Create(LoadingMenuType.LoadWorld);
    }
}