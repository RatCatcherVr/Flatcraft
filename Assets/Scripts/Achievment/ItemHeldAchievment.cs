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
#if !DISABLESTEAMWORKS
        UnityEngine.Debug.Log("Granted achievement: " + achievementId);
#endif
    }
#endif
}