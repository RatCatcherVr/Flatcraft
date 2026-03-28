using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemHeldAchievement : Achievement
{
    public Material[] materialCriteria;

#if !DISABLESTEAMWORKS
    protected override void TrackingLoop()
    {
        PlayerInstance localPlayerInstance = PlayerInstance.localPlayerInstance;
        if (!localPlayerInstance) return;

        Player playerEntity = localPlayerInstance.playerEntity;
        if (!playerEntity) return;

        PlayerInventory inventory = playerEntity.GetInventoryHandler().GetInventory();
        // inventory.Contains is now available via base Inventory class
        if (inventory == null) return;

        foreach (Material matAlternative in materialCriteria)
        {
            if (inventory.Contains(matAlternative))
            {
                GrantAchievement();
                return;
            }
        }
    }

    private void GrantAchievement()
    {
        UnityEngine.Debug.Log("Granted achievement: " + achievementId);
    }
#endif
}