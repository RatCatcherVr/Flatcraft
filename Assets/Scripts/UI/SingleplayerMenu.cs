using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System;

public class SingleplayerMenu : MonoBehaviour
{
    public Transform list;
    public GameObject worldPrefab;
    public Button playButton;
    public Button deleteButton;

    public List<World> worlds = new List<World>();
    public int selectedWorld = -1;

    void Start()
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

        foreach (Transform child in list)
            Destroy(child.gameObject);

        for (int i = 0; i < worlds.Count; i++)
        {
            int index = i;
            GameObject obj = Instantiate(worldPrefab, list, false);
            var worldNameText = obj.GetComponentInChildren<Text>();
            if (worldNameText != null) worldNameText.text = worlds[i].name;

            var btn = obj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => selectedWorld = index);
            }
        }
    }

    public static List<World> GetWorlds()
    {
        // iOS-safe world loading
        string savesPath = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(savesPath))
            Directory.CreateDirectory(savesPath);

        return World.LoadWorlds();
    }

    public void Play()
    {
        if (selectedWorld < 0 || selectedWorld >= worlds.Count) return;
        WorldManager.world = worlds[selectedWorld];
        LoadingMenu.Create(LoadingMenuType.LoadWorld);
    }

    public void Create()
    {
        string savesPath = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(savesPath))
            Directory.CreateDirectory(savesPath);

        int worldIndex = 1;
        string worldName;
        do
        {
            worldName = $"World {worldIndex++}";
        } while (World.WorldExists(worldName));

        World newWorld = new World(worldName, new System.Random().Next());
        newWorld.SaveData();
        WorldManager.world = newWorld;

        StartCoroutine(LoadWorldScene());
    }

    private System.Collections.IEnumerator LoadWorldScene()
    {
        yield return null; // wait a frame for iOS
        LoadingMenu.Create(LoadingMenuType.LoadWorld);
    }

    public void Delete()
    {
        if (selectedWorld < 0 || selectedWorld >= worlds.Count) return;
        DeleteWorldMenu.SelectedWorld = worlds[selectedWorld];
        Instantiate(Resources.Load<GameObject>("DeleteWorldMenuPrefab"));
    }
}