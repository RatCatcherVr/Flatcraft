using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Mirror;
using UnityEngine;

public class SaveManager : NetworkBehaviour
{
    public static float AutosaveDuration = 2;
    public static List<BlockChange> unsavedBlockChanges = new List<BlockChange>();

    private void Start()
    {
        if (isServer)
            StartCoroutine(SaveLoop());
    }

    private IEnumerator SaveLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(AutosaveDuration);

            //Save Block Changes
            if (unsavedBlockChanges.Count > 0)
            {
                List<BlockChange> blockChangesCopy = new List<BlockChange>(unsavedBlockChanges);
                unsavedBlockChanges.Clear();
                Thread worldThread = new Thread(() => { SaveBlockChanges(blockChangesCopy); });
                worldThread.Start();

                while (worldThread.IsAlive)
                    yield return new WaitForSeconds(0.1f);
            }

            //Save Entities
            List<Entity> entities = new List<Entity>(Entity.entities);

            Thread entityThread = new Thread(() => { SaveEntities(entities); });
            entityThread.Start();

            while (entityThread.IsAlive)
                yield return new WaitForSeconds(0.1f);

            //Delete no longer present entities
            foreach (Chunk chunk in WorldManager.instance.chunks.Values)
            {
                chunk.DeleteNoLongerPresentEntitiesSaves();
            }
        }
    }

    public static void SaveEntities(List<Entity> entities)
    {
        foreach (Entity e in entities)
            try
            {
                e.Save();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("Error in saving entity: " + ex.StackTrace);
            }
    }

    public static void SaveBlockChanges(List<BlockChange> changes)
    {
        //Get all changed chunks
        Dictionary<ChunkPosition, List<BlockChange>> chunkBlockChanges = new Dictionary<ChunkPosition, List<BlockChange>>();

        foreach (BlockChange blockChange in changes)
        {
            ChunkPosition chunkPos = new ChunkPosition(blockChange.location);

            if (!chunkBlockChanges.ContainsKey(chunkPos))
                chunkBlockChanges.Add(chunkPos, new List<BlockChange>());

            chunkBlockChanges[chunkPos].Add(blockChange);
        }

        foreach (ChunkPosition changedChunk in chunkBlockChanges.Keys)
        {
            Dictionary<Location, BlockState> chunkBlockStates = new Dictionary<Location, BlockState>();

            if (!changedChunk.HasBeenSaved())
                changedChunk.CreateChunkPath();

            string changedChunkFilePath =
                UnityEngine.Application.persistentDataPath + "/chunks/" + changedChunk.dimension + "/" + changedChunk.chunkX;

            if (Directory.Exists(changedChunkFilePath + "/blocks"))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(changedChunkFilePath + "/blocks"))
                    {
                        Location lineLocation = new Location(
                            int.Parse(line.Split('*')[0].Split(',')[0]),
                            int.Parse(line.Split('*')[0].Split(',')[1])
                        );
                        string lineBlockSaveString = line.Split('*')[1] + "*" + line.Split('*')[2];
                        BlockState blockState = new BlockState(lineBlockSaveString);

                        chunkBlockStates[lineLocation] = blockState;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Error loading block state: " + e.Message + e.StackTrace);
                }
            }

            List<BlockChange> newBlockChangesForChunk = chunkBlockChanges[changedChunk];

            foreach (var blockChange in newBlockChangesForChunk)
            {
                chunkBlockStates[blockChange.location] = blockChange.newBlockState;
            }

            Directory.CreateDirectory(changedChunkFilePath);

            using (TextWriter c = new StreamWriter(changedChunkFilePath + "/blocks"))
            {
                foreach (Location location in chunkBlockStates.Keys)
                    c.WriteLine(location.x + "," + location.y + "*" + chunkBlockStates[location].GetSaveString());
            }
        }
    }
}

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