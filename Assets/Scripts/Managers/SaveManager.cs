using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mirror;
using UnityEngine;

public class SaveManager : NetworkBehaviour
{
    public static float AutosaveDuration = 2f;
    public static List<BlockChange> unsavedBlockChanges = new List<BlockChange>();

    private void Start()
    {
        if (isServer)
            StartCoroutine(SaveLoop());
    }

    private void OnApplicationQuit()
    {
        if (isServer)
            StartCoroutine(SaveAllImmediately());
    }

    private IEnumerator SaveLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(AutosaveDuration);

            if (unsavedBlockChanges.Count > 0)
            {
                List<BlockChange> blockChangesCopy = new List<BlockChange>(unsavedBlockChanges);
                unsavedBlockChanges.Clear();
                yield return StartCoroutine(SaveBlockChangesCoroutine(blockChangesCopy));
            }

            List<Entity> entities = new List<Entity>(Entity.entities);
            yield return StartCoroutine(SaveEntitiesCoroutine(entities));

            foreach (Chunk chunk in WorldManager.instance.chunks.Values)
            {
                chunk.DeleteNoLongerPresentEntitiesSaves();
            }
        }
    }

    private IEnumerator SaveAllImmediately()
    {
        if (unsavedBlockChanges.Count > 0)
        {
            List<BlockChange> blockChangesCopy = new List<BlockChange>(unsavedBlockChanges);
            unsavedBlockChanges.Clear();
            yield return StartCoroutine(SaveBlockChangesCoroutine(blockChangesCopy));
        }

        List<Entity> entities = new List<Entity>(Entity.entities);
        yield return StartCoroutine(SaveEntitiesCoroutine(entities));
    }

    private IEnumerator SaveEntitiesCoroutine(List<Entity> entities)
    {
        foreach (Entity e in entities)
        {
            try
            {
                e.Save();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("Error saving entity: " + ex.StackTrace);
            }
            yield return null;
        }
    }

    private IEnumerator SaveBlockChangesCoroutine(List<BlockChange> changes)
    {
        Dictionary<ChunkPosition, List<BlockChange>> chunkBlockChanges = new Dictionary<ChunkPosition, List<BlockChange>>();
        foreach (BlockChange blockChange in changes)
        {
            ChunkPosition chunkPos = new ChunkPosition(blockChange.location);
            if (!chunkBlockChanges.ContainsKey(chunkPos))
                chunkBlockChanges.Add(chunkPos, new List<BlockChange>());
            chunkBlockChanges[chunkPos].Add(blockChange);
        }

        foreach (var kvp in chunkBlockChanges)
        {
            ChunkPosition chunkPos = kvp.Key;
            List<BlockChange> newChanges = kvp.Value;

            string chunkDir = Path.Combine(UnityEngine.Application.persistentDataPath, "chunks", chunkPos.dimension.ToString(), chunkPos.chunkX.ToString());
            if (!Directory.Exists(chunkDir)) Directory.CreateDirectory(chunkDir);

            Dictionary<Location, BlockState> chunkBlockStates = new Dictionary<Location, BlockState>();
            string blockFile = Path.Combine(chunkDir, "blocks");

            if (File.Exists(blockFile))
            {
                string[] lines = File.ReadAllLines(blockFile);
                foreach (string line in lines)
                {
                    string[] split = line.Split('*');
                    string[] pos = split[0].Split(',');
                    Location loc = new Location(int.Parse(pos[0]), int.Parse(pos[1]));
                    BlockState state = new BlockState(split[1] + "*" + split[2]);
                    chunkBlockStates[loc] = state;
                }
            }

            foreach (var blockChange in newChanges)
            {
                chunkBlockStates[blockChange.location] = blockChange.newBlockState;
            }

            using (TextWriter writer = new StreamWriter(blockFile))
            {
                foreach (var kv in chunkBlockStates)
                {
                    writer.WriteLine(kv.Key.x + "," + kv.Key.y + "*" + kv.Value.GetSaveString());
                }
            }

            yield return null;
        }
    }
}

[Serializable]
public struct BlockChange
{
    public Location location;
    public BlockState newBlockState;

    public BlockChange(Location location, BlockState newBlockState)
    {
        this.location = location;
        this.newBlockState = newBlockState;
    }
}