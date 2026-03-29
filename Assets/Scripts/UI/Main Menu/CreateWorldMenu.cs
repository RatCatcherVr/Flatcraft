using System;
using UnityEngine;
using Random = System.Random;

public class CreateWorldMenu : MonoBehaviour
{
    public World world = new World();
    public GameObject singleplayerMenuPrefab;

    private void Start()
    {
        AutoCreateWorld();
    }

    private void AutoCreateWorld()
    {
        string baseName = "World";
        string uniqueName = baseName;
        int counter = 1;
        while (World.WorldExists(uniqueName))
        {
            uniqueName = baseName + counter;
            counter++;
        }
        world.name = uniqueName;

        world.seed = new Random().Next();

        world.SaveData();

        WorldManager.world = world;
        MultiplayerManager.HostGameAsync();
        LoadingMenu.Create(LoadingMenuType.LoadWorld);

        Destroy(gameObject);
    }

    public void Cancel()
    {
        Instantiate(singleplayerMenuPrefab);
        Destroy(gameObject);
    }
}