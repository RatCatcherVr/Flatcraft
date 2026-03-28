using System;
using Mirror;
using UnityEngine;

[Serializable]
public class FurnaceInventory : Inventory
{
    [SyncVar] public float fuelLeft;
    [SyncVar] public float highestFuel;
    [SyncVar] public int smeltingProgress;

    [Server]
    public override void Update()
    {
        base.Update();
        if (!isServer) return;

        if (Time.time % 1f - Time.deltaTime <= 0)
        {
            SmeltTick();
        }
    }

    [Server]
    public static Inventory CreatePreset() => Create("FurnaceInventory", 3, "Furnace");

    public int GetFuelSlot() => 0;
    public int GetIngredientSlot() => 1;
    public int GetResultSlot() => 2;

    [Server]
    public void SmeltTick()
    {
        SmeltingRecipe curRecipe = GetRecipe();
        bool canSmelt = curRecipe != null &&
                        (GetItem(GetResultSlot()).material == curRecipe.result.material || GetItem(GetResultSlot()).material == Material.Air) &&
                        GetItem(GetResultSlot()).Amount < MaxStackSize;

        if (fuelLeft <= 0 && canSmelt)
        {
            Material fuelMat = GetItem(GetFuelSlot()).material;
            if (SmeltingRecipe.Fuels.ContainsKey(fuelMat))
            {
                fuelLeft = SmeltingRecipe.Fuels[fuelMat];
                highestFuel = fuelLeft;
                ItemStack newFuelItem = GetItem(GetFuelSlot());
                newFuelItem.Amount--;
                SetItem(GetFuelSlot(), newFuelItem);
            }
        }

        if (fuelLeft > 0)
        {
            fuelLeft--;
            if (canSmelt)
            {
                smeltingProgress++;
                if (smeltingProgress >= SmeltingRecipe.smeltTime)
                    FillSmeltingResult();
            }
            else smeltingProgress = 0;
        }
        else
        {
            highestFuel = 0;
            smeltingProgress = 0;
        }
    }

    [Server]
    public void FillSmeltingResult()
    {
        SmeltingRecipe curRecipe = GetRecipe();
        if (curRecipe == null) return;
        ItemStack resStack = GetItem(GetResultSlot());
        ItemStack ingStack = GetItem(GetIngredientSlot());
        resStack.material = curRecipe.result.material;
        resStack.Amount += 1;
        ingStack.Amount--;
        SetItem(GetResultSlot(), resStack);
        SetItem(GetIngredientSlot(), ingStack);
        smeltingProgress = 0;
    }

    public SmeltingRecipe GetRecipe()
    {
        if (GetItem(GetIngredientSlot()).Amount <= 0) return null;
        return SmeltingRecipe.FindRecipeByIngredient(GetItem(GetIngredientSlot()).material);
    }
}