using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CircletExtended.CircletExtended;

namespace CircletExtended
{
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    public static class ObjectDB_Awake_CircletStats
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ObjectDB __instance, ref List<Recipe> ___m_recipes)
        {
            if (!modEnabled.Value)
                return;

            if (!getFeaturesByUpgrade.Value)
                return;

            GameObject prefab = __instance.GetItemPrefab(itemHashHelmetDverger);
            if (prefab == null)
                return;

            if (___m_recipes.RemoveAll(x => x.name == itemNameHelmetDverger) > 0)
                LogInfo($"Removed recipe {itemNameHelmetDverger}");

            CraftingStation station = __instance.m_recipes.FirstOrDefault(rec => rec.m_craftingStation?.m_name == "$piece_forge")?.m_craftingStation;

            ItemDrop item = prefab.GetComponent<ItemDrop>();
            PatchCircletItemData(item.m_itemData);

            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = itemNameHelmetDverger;
            recipe.m_amount = 1;
            recipe.m_minStationLevel = 3;
            recipe.m_item = item;
            recipe.m_enabled = true;

            if (station != null)
                recipe.m_craftingStation = station;

            recipe.m_resources = new Piece.Requirement[1] {new Piece.Requirement()
                {
                    m_amount = 1,
                    m_resItem = item,
                }};

            ___m_recipes.Add(recipe);

            recipeRequirements.Clear();
            for (int i = 0; i <= 5; i++)
            {
                List<Piece.Requirement> requirements = new List<Piece.Requirement>();
                switch (i)
                {
                    case 2:
                        requirements.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab("Resin").GetComponent<ItemDrop>(), m_amount = 20 });
                        requirements.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab("LeatherScraps").GetComponent<ItemDrop>(), m_amount = 10 });
                        requirements.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab("IronNails").GetComponent<ItemDrop>(), m_amount = 10 });
                        requirements.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab("Chain").GetComponent<ItemDrop>(), m_amount = 1 });
                        break;
                    case 3:
                        requirements.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab("Thunderstone").GetComponent<ItemDrop>(), m_amount = 5 });
                        requirements.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab("Silver").GetComponent<ItemDrop>(), m_amount = 1 });
                        requirements.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab("JuteRed").GetComponent<ItemDrop>(), m_amount = 2 });
                        break;
                    case 4:
                        requirements.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab("Demister").GetComponent<ItemDrop>(), m_amount = 1 });
                        requirements.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab("BlackCore").GetComponent<ItemDrop>(), m_amount = 1 });
                        break;

                    default:
                        requirements.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(itemNameHelmetDverger).GetComponent<ItemDrop>(), m_amount = 1 });
                        break;
                }
                recipeRequirements.Add(i, requirements.ToArray());
            }

            demisterForceField = __instance.GetItemPrefab("Demister")?.transform.Find(forceFieldDemisterName)?.gameObject;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
    public static class Player_AddKnownItem_CircletStats
    {
        private static void Postfix(ref ItemDrop.ItemData item)
        {
            if (!modEnabled.Value)
                return;

            if (!getFeaturesByUpgrade.Value)
                return;

            if (item.m_shared.m_name != itemDropNameHelmetDverger)
                return;

            PatchCircletItemData(item);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public class Player_OnSpawned_CircletStats
    {
        public static void Postfix(Player __instance)
        {
            if (!modEnabled.Value)
                return;

            if (!getFeaturesByUpgrade.Value)
                return;

            if (__instance != Player.m_localPlayer)
                return;

            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>();
            __instance.GetInventory().GetAllItems(itemDropNameHelmetDverger, items);

            foreach (ItemDrop.ItemData item in items)
                PatchCircletItemData(item);
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load))]
    public class Inventory_Load_CircletStats
    {
        public static void Postfix(Inventory __instance)
        {
            if (!modEnabled.Value)
                return;

            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>();
            __instance.GetAllItems(itemDropNameHelmetDverger, items);

            foreach (ItemDrop.ItemData item in items)
                PatchCircletItemData(item);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
    public static class ItemDrop_Start_CircletStats
    {
        private static void Postfix(ref ItemDrop __instance)
        {
            if (!modEnabled.Value)
                return;

            if (!getFeaturesByUpgrade.Value)
                return;

            if (__instance.GetPrefabName(__instance.name) != itemNameHelmetDverger)
                return;

            PatchCircletItemData(__instance.m_itemData);
        }
    }

    [HarmonyPatch(typeof(Piece.Requirement), nameof(Piece.Requirement.GetAmount))]
    public class PieceRequirement_GetAmount_CircletUpgrade
    {
        public static void Postfix(Piece.Requirement __instance, int qualityLevel, ref int __result)
        {
            if (!modEnabled.Value)
                return;

            if (!getFeaturesByUpgrade.Value)
                return;

            if (__instance.m_resItem.GetPrefabName(__instance.m_resItem.name) == itemNameHelmetDverger)
                __result = qualityLevel > 1 ? 0 : 1;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    public class InventoryGui_DoCrafting_CircletUpgrade
    {
        private static bool PatchMethod(ItemDrop.ItemData ___m_craftUpgradeItem, Recipe ___m_craftRecipe)
        {
            if (!modEnabled.Value)
                return false;

            if (!getFeaturesByUpgrade.Value)
                return false;

            if (___m_craftRecipe == null || ___m_craftUpgradeItem == null)
                return false;

            if (___m_craftUpgradeItem.m_shared.m_name != itemDropNameHelmetDverger)
                return false;

            return true;
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(Player player, ref Recipe ___m_craftRecipe, ref KeyValuePair<bool, Piece.Requirement[]> __state, ItemDrop.ItemData ___m_craftUpgradeItem)
        {   
            if (!PatchMethod(___m_craftUpgradeItem, ___m_craftRecipe))
                return;

            int quality = ___m_craftUpgradeItem.m_quality + 1;

            __state = new KeyValuePair<bool, Piece.Requirement[]>(player.m_noPlacementCost, ___m_craftRecipe.m_resources.ToArray());

            ___m_craftRecipe.m_resources = recipeRequirements[quality].ToArray();

            player.m_noPlacementCost = true;
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Player player, ref Recipe ___m_craftRecipe, KeyValuePair<bool, Piece.Requirement[]> __state, ItemDrop.ItemData ___m_craftUpgradeItem)
        {
            if (!PatchMethod(___m_craftUpgradeItem, ___m_craftRecipe))
                return;

            player.m_noPlacementCost = __state.Key;
            if (!player.m_noPlacementCost)
                player.ConsumeResources(___m_craftRecipe.m_resources, 1);

            ___m_craftRecipe.m_resources = __state.Value.ToArray();

            PatchCircletItemData(___m_craftUpgradeItem);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirementList))]
    public class InventoryGui_SetupRequirementList_CircletUpgrade
    {
        private static bool PatchMethod(KeyValuePair<Recipe, ItemDrop.ItemData> ___m_selectedRecipe)
        {
            if (!modEnabled.Value)
                return false;

            if (!getFeaturesByUpgrade.Value)
                return false;

             if (___m_selectedRecipe.Key.m_item.GetPrefabName(___m_selectedRecipe.Key.m_item.name) != itemNameHelmetDverger)
                return false;

            return true;
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(ref int quality, ref KeyValuePair<Recipe, ItemDrop.ItemData> ___m_selectedRecipe, ref KeyValuePair<int, Piece.Requirement[]> __state, Recipe ___m_craftRecipe)
        {
            if (!PatchMethod(___m_selectedRecipe))
                return;

            __state = new KeyValuePair<int, Piece.Requirement[]>(quality, ___m_selectedRecipe.Key.m_resources.ToArray());

            ___m_selectedRecipe.Key.m_resources = recipeRequirements[quality].ToArray();

            quality = 1;
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref int quality, ref KeyValuePair<Recipe, ItemDrop.ItemData> ___m_selectedRecipe, KeyValuePair<int, Piece.Requirement[]> __state, Recipe ___m_craftRecipe)
        {
            if (!PatchMethod(___m_selectedRecipe))
                return;

            ___m_selectedRecipe.Key.m_resources = __state.Value.ToArray();

            quality = __state.Key;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
    public class Player_HaveRequirementItems_CircletUpgrade
    {
        private static bool PatchMethod(bool discover, Recipe piece)
        {
            if (!modEnabled.Value)
                return false;

            if (!getFeaturesByUpgrade.Value)
                return false;

            if (discover)
                return false;

            if (piece.m_item.GetPrefabName(piece.m_item.name) != itemNameHelmetDverger)
                return false;

            return true;
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(ref Recipe piece, bool discover, ref int qualityLevel, ref KeyValuePair<int, Piece.Requirement[]> __state)
        {
            if (!PatchMethod(discover, piece))
                return;

            __state = new KeyValuePair<int, Piece.Requirement[]>(qualityLevel, piece.m_resources.ToArray());

            piece.m_resources = recipeRequirements[qualityLevel].ToArray();

            qualityLevel = 1;
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref Recipe piece, bool discover, ref int qualityLevel, KeyValuePair<int, Piece.Requirement[]> __state)
        {
            if (!PatchMethod(discover, piece))
                return;

            piece.m_resources = __state.Value.ToArray();

            qualityLevel = __state.Key;
        }
    }    
}
