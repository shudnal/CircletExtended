using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static CircletExtended.CircletExtended;

namespace CircletExtended
{
    public static class CircletItem
    {
        public const string itemNameHelmetDverger = "HelmetDverger";
        public const string itemDropNameHelmetDverger = "$item_helmet_dverger";
        public const string itemDropNameHelmetDvergerLvl2 = "$item_helmet_dverger_quality_lvl_2";
        public static int itemHashHelmetDverger = itemNameHelmetDverger.GetStableHashCode();

        public static GameObject circletPrefab;

        public static List<string> helmetWhiteList = new List<string>();
        public static List<string> helmetBlackList = new List<string>();
        public static bool helmetListFilled = false;

        public const int maxQuality = 4;

        public static Recipe recipe;
        
        public static Dictionary<int, Piece.Requirement[]> recipeRequirements = new Dictionary<int, Piece.Requirement[]>();

        public static void UpdateCompatibleHelmetLists()
        {
            helmetWhiteList.Clear();
            circletHelmetWhiteList.Value.Split(',').Select(p => p.Trim().ToLower()).Where(p => !string.IsNullOrWhiteSpace(p)).Do(helmetWhiteList.Add);
           
            helmetBlackList.Clear();
            circletHelmetBlackList.Value.Split(',').Select(p => p.Trim().ToLower()).Where(p => !string.IsNullOrWhiteSpace(p)).Do(helmetBlackList.Add);

            helmetListFilled = helmetWhiteList.Count + helmetBlackList.Count > 0;
        }

        internal static bool CanCircletBeEquippedWithHelmet(ItemDrop.ItemData helmet)
        {
            if (helmet == null)
                return true;

            if (!helmetListFilled)
                return true;

            if (helmetWhiteList.Contains(helmet.m_shared.m_name.ToLower()))
                return true;

            if (helmetWhiteList.Contains(helmet.m_dropPrefab?.name.ToLower()))
                return true;

            if (helmetWhiteList.Count > 0)
                return false;

            if (helmetBlackList.Contains(helmet.m_shared.m_name.ToLower()))
                return false;

            if (helmetBlackList.Contains(helmet.m_dropPrefab?.name.ToLower()))
                return false;

            return true;
        }

        internal static ItemDrop.ItemData.ItemType GetItemType()
        {
            return (ItemDrop.ItemData.ItemType)itemSlotType.Value;
        }

        internal static bool IsCircletType(ItemDrop.ItemData item) => item != null && item.m_shared.m_itemType == GetItemType();

        internal static bool IsCircletItem(ItemDrop item)
        {
            return item != null && (IsCircletItemName(item.GetPrefabName(item.name)) || IsCircletItemData(item.m_itemData));
        }

        internal static bool IsCircletItemData(ItemDrop.ItemData item)
        {
            return item != null && (item.m_dropPrefab != null && IsCircletItemName(item.m_dropPrefab.name) || IsCircletItemDropName(item.m_shared.m_name));
        }

        internal static bool IsCircletItem(ItemDrop.ItemData item)
        {
            return IsCircletItemData(item) && IsCircletType(item);
        }

        internal static bool IsCircletItemDropName(string name)
        {
            return name == itemDropNameHelmetDverger;
        }

        internal static bool IsCircletItemName(string name)
        {
            return name == itemNameHelmetDverger;
        }

        public static bool IsCircletSlotKnown()
        {
            if (!Player.m_localPlayer || Player.m_localPlayer.m_isLoading)
                return true;

            return Player.m_localPlayer.IsKnownMaterial(itemDropNameHelmetDverger) && (!getFeaturesByUpgrade.Value || Player.m_localPlayer.IsKnownMaterial(itemDropNameHelmetDvergerLvl2));
        }

        public static bool IsCircletSlotAvailable() => itemSlotExtraSlots.Value && (!itemSlotExtraSlotsDiscovery.Value || IsCircletSlotKnown());

        internal static void PatchCircletItemData(ItemDrop.ItemData item, bool inventoryItemUpdate = true)
        {
            if (item == null)
                return;

            item.m_shared.m_maxQuality = getFeaturesByUpgrade.Value ? 4 : 1;
            item.m_shared.m_durabilityPerLevel = getFeaturesByUpgrade.Value ? fuelPerLevel.Value : 100;
            
            item.m_shared.m_useDurability = UseFuel() || item.GetDurabilityPercentage() != 1f || item.m_quality >= 3;
            item.m_shared.m_maxDurability = UseFuel() ? fuelMinutes.Value : 1000;
            item.m_shared.m_useDurabilityDrain = UseFuel() ? 1f : 0f;
            item.m_shared.m_durabilityDrain = UseFuel() ? Time.fixedDeltaTime * (50f / 60f) : 0f;
            item.m_shared.m_destroyBroken = false;
            item.m_shared.m_canBeReparied = true;

            if (!inventoryItemUpdate || item.m_durability > item.GetMaxDurability())
                item.m_durability = item.GetMaxDurability();

            if (enablePutOnTop.Value)
            {
                if (getFeaturesByUpgrade.Value && item.m_quality >= 2 || !getFeaturesByUpgrade.Value)
                {
                    item.m_shared.m_itemType = GetItemType();
                    item.m_shared.m_attachOverride = ItemDrop.ItemData.ItemType.Helmet;
                }
            }
        }

        internal static void PatchInventory(Inventory inventory)
        {
            if (inventory == null)
                return;

            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>();
            inventory.GetAllItems(itemDropNameHelmetDverger, items);

            foreach (ItemDrop.ItemData item in items)
                PatchCircletItemData(item);
        }

        internal static void PatchCircletItemOnConfigChange()
        {
            PatchCircletItemData(circletPrefab?.GetComponent<ItemDrop>()?.m_itemData, inventoryItemUpdate: false);

            PatchInventory(Player.m_localPlayer?.GetInventory());
        }

        internal static bool UseFuel()
        {
            return fuelMinutes.Value > 0;
        }

        public static bool IsCircletLightEnabled(this ItemDrop.ItemData item)
        {
            return DvergerLightController.IsCircletLightEnabled(item);
        }

        internal static void FillRecipe()
        {
            if (!ObjectDB.instance)
                return;

            if (ObjectDB.instance.m_recipes.RemoveAll(x => x is Recipe recipe && IsCircletItemName(recipe.name)) > 0)
                LogInfo($"Recipe removed {itemNameHelmetDverger}");

            circletPrefab = ObjectDB.instance.GetItemPrefab(itemHashHelmetDverger);
            if (circletPrefab == null)
                return;

            ItemDrop item = circletPrefab.GetComponent<ItemDrop>();
            PatchCircletItemData(item.m_itemData, inventoryItemUpdate: false);

            if (recipe != null)
                UnityEngine.Object.Destroy(recipe);

            FillRecipeRequirements();

            CraftingStation forge = ObjectDB.instance.m_recipes.FirstOrDefault(rec => rec.m_craftingStation?.m_name == "$piece_forge")?.m_craftingStation;
            CraftingStation craftingStation = ObjectDB.instance.m_recipes.FirstOrDefault(rec => rec.m_craftingStation?.m_name == circletRecipeCraftingStation.Value)?.m_craftingStation;
            CraftingStation repairStation = ObjectDB.instance.m_recipes.FirstOrDefault(rec => rec.m_craftingStation?.m_name == circletRecipeRepairStation.Value)?.m_craftingStation;
            
            recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = itemNameHelmetDverger;
            recipe.m_amount = 1;
            recipe.m_minStationLevel = Math.Min(circletRecipeCraftingStationLvl.Value, circletRecipeRepairStationLvl.Value); // Actual crafting level is overriden
            recipe.m_item = item;
            recipe.m_enabled = true;

            recipe.m_craftingStation = craftingStation ?? forge;
            recipe.m_repairStation = repairStation ?? null;

            recipe.m_resources = recipeRequirements[1];

            ObjectDB.instance.m_recipes.Add(recipe);
            LogInfo($"Recipe added {itemNameHelmetDverger}");
        }

        private static void FillRecipeRequirements()
        {
            recipeRequirements.Clear();
            for (int quality = 0; quality <= 5; quality++)
                recipeRequirements.Add(quality, GetRequirements(quality));
        }

        private static string GetRecipe(int quality)
        {
            return quality switch
            {
                1 => circletRecipeQuality1.Value,
                2 => circletRecipeQuality2.Value,
                3 => circletRecipeQuality3.Value,
                4 => circletRecipeQuality4.Value,
                _ => ""
            };
        }

        private static Piece.Requirement[] GetRequirements(int quality)
        {
            List<Piece.Requirement> requirements = new List<Piece.Requirement>();

            foreach (string requirement in GetRecipe(quality).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] req = requirement.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (req.Length != 2)
                    continue;

                int amount = int.Parse(req[1]);
                if (amount <= 0)
                    continue;

                var prefab = ObjectDB.instance.GetItemPrefab(req[0].Trim());
                if (prefab == null)
                    continue;

                requirements.Add(new Piece.Requirement()
                {
                    m_amount = amount,
                    m_resItem = prefab.GetComponent<ItemDrop>(),
                });
            };

            return requirements.ToArray();
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class ObjectDB_Awake_CircletStats
        {
            private static void Postfix()
            {
                DvergerLightController.RegisterEffects();

                FillRecipe();

                UpdateCompatibleHelmetLists();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
        public static class Player_AddKnownItem_CircletRecipeAvailableAfterAcquiring
        {
            private static void Prefix(Player __instance, ItemDrop.ItemData item, ref bool __state)
            {
                if (!IsCircletItemData(item))
                    return;

                if (__instance.IsKnownMaterial(itemDropNameHelmetDverger))
                {
                    if (!__instance.IsRecipeKnown(itemDropNameHelmetDverger) && ObjectDB.instance.m_recipes.FirstOrDefault(x => IsCircletItemName(x.name)) is Recipe recipe)
                        __instance.AddKnownRecipe(recipe);
                }
                else
                    __state = true;
            }

            private static void Postfix(Player __instance, ItemDrop.ItemData item, bool __state)
            {
                if (__state && ObjectDB.instance.m_recipes.FirstOrDefault(x => IsCircletItemName(x.name)) is Recipe recipe)
                    __instance.AddKnownRecipe(recipe);

                if (enablePutOnTop.Value && IsCircletItemData(item) && (item.m_quality > 1 || !getFeaturesByUpgrade.Value) && __instance.IsKnownMaterial(itemDropNameHelmetDverger) && !__instance.IsKnownMaterial(itemDropNameHelmetDvergerLvl2))
                    __instance.m_knownMaterial.Add(itemDropNameHelmetDvergerLvl2);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public static class Player_OnSpawned_CircletStats
        {
            public static void Postfix(Player __instance)
            {
                if (!getFeaturesByUpgrade.Value)
                    return;

                if (__instance != Player.m_localPlayer)
                    return;

                PatchInventory(__instance.GetInventory());
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load))]
        public static class Inventory_Load_CircletStats
        {
            public static void Postfix(Inventory __instance)
            {
                PatchInventory(__instance);
            }
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
        public static class ItemDrop_Start_CircletStats
        {
            private static void Postfix(ItemDrop __instance)
            {
                if (!getFeaturesByUpgrade.Value)
                    return;

                if (!IsCircletItem(__instance))
                    return;

                PatchCircletItemData(__instance.m_itemData);
            }
        }

        [HarmonyPatch(typeof(Piece.Requirement), nameof(Piece.Requirement.GetAmount))]
        public static class PieceRequirement_GetAmount_CircletUpgrade
        {
            public static void Postfix(Piece.Requirement __instance, int qualityLevel, ref int __result)
            {
                if (!getFeaturesByUpgrade.Value)
                    return;

                if (IsCircletItem(__instance.m_resItem))
                    __result = qualityLevel > 1 ? 0 : 1;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        public static class InventoryGui_DoCrafting_CircletUpgrade
        {
            private static bool PatchMethod(ItemDrop.ItemData ___m_craftUpgradeItem, Recipe ___m_craftRecipe)
            {
                if (!getFeaturesByUpgrade.Value)
                    return false;

                if (___m_craftRecipe == null || ___m_craftUpgradeItem == null)
                    return false;

                if (!IsCircletItemData(___m_craftUpgradeItem))
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

                PatchCircletItemData(___m_craftUpgradeItem, inventoryItemUpdate: false);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirementList))]
        public static class InventoryGui_SetupRequirementList_CircletUpgrade
        {
            private static bool PatchMethod(KeyValuePair<Recipe, ItemDrop.ItemData> ___m_selectedRecipe)
            {
                if (!getFeaturesByUpgrade.Value)
                    return false;

                return IsCircletItem(___m_selectedRecipe.Key.m_item);
            }

            [HarmonyPriority(Priority.First)]
            public static void Prefix(ref int quality, ref KeyValuePair<Recipe, ItemDrop.ItemData> ___m_selectedRecipe, ref KeyValuePair<int, Piece.Requirement[]> __state)
            {
                if (!PatchMethod(___m_selectedRecipe))
                    return;

                __state = new KeyValuePair<int, Piece.Requirement[]>(quality, ___m_selectedRecipe.Key.m_resources.ToArray());

                ___m_selectedRecipe.Key.m_resources = recipeRequirements[quality].ToArray();

                quality = 1;
            }

            [HarmonyPriority(Priority.Last)]
            public static void Postfix(ref int quality, ref KeyValuePair<Recipe, ItemDrop.ItemData> ___m_selectedRecipe, KeyValuePair<int, Piece.Requirement[]> __state)
            {
                if (!PatchMethod(___m_selectedRecipe))
                    return;

                ___m_selectedRecipe.Key.m_resources = __state.Value.ToArray();

                quality = __state.Key;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
        public static class Player_HaveRequirementItems_CircletUpgrade
        {
            private static bool PatchMethod(bool discover, Recipe piece)
            {
                if (!getFeaturesByUpgrade.Value)
                    return false;

                if (discover)
                    return false;

                return IsCircletItem(piece.m_item);
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

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipment))]
        public static class Humanoid_UpdateEquipment_CircletEquipmentDrain
        {
            public static void Postfix(Humanoid __instance, float dt)
            {
                if (__instance.IsPlayer() && __instance.GetCirclet() is ItemDrop.ItemData circlet && circlet != __instance.m_helmetItem)
                    __instance.DrainEquipedItemDurability(circlet, dt * DvergerLightController.GetCircletDrainMultiplier(circlet));
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DrainEquipedItemDurability))]
        public static class Humanoid_DrainEquipedItemDurability_CircletEquipmentDrain
        {
            [HarmonyPriority(Priority.First)]
            public static void Prefix(Humanoid __instance, ItemDrop.ItemData item, ref float dt, ref float __state)
            {
                if (!IsCircletItemData(item))
                    return;

                if (UseFuel() && item.IsCircletLightEnabled() && __instance.IsPlayer() && (__instance as Player).GetCurrentCraftingStation() == null)
                    return;

                __state = dt; 
                dt = 0f;
            }

            [HarmonyPriority(Priority.First)]
            public static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref float dt, float __state)
            {
                if (__state != 0f)
                    dt = __state;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
        private static class ItemDropItemData_GetTooltip_ItemTooltip
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(ItemDrop.ItemData item, ref string __result)
            {
                if (IsCircletItemData(item) && UseFuel())
                    __result = __result.Replace("$item_durability", "$piece_fire_fuel");
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetWornItems))]
        public static class Inventory_GetWornItems_CircletAlwaysLastToRepair
        {
            public static void Postfix(Inventory __instance, List<ItemDrop.ItemData> worn)
            {
                if (worn.Count > 0 && __instance == Player.m_localPlayer?.GetInventory() && UseFuel())
                    for (int i = worn.Count - 1; i >= 0; i--)
                        if (worn[i] is ItemDrop.ItemData item && IsCircletItemData(item) && item.m_equipped && Player.m_localPlayer.IsItemEquiped(item))
                        {
                            worn.Add(item);
                            worn.RemoveAt(i);
                        }
            }
        }

        [HarmonyPatch(typeof(Recipe), nameof(Recipe.GetRequiredStationLevel))]
        public static class Recipe_GetRequiredStationLevel_CraftingStationLevel
        {
            private static void Prefix(Recipe __instance, ref int __state)
            {
                __state = -1;
                if (__instance != recipe)
                    return;

                __state = __instance.m_minStationLevel;
                __instance.m_minStationLevel = circletRecipeCraftingStationLvl.Value;
            }

            private static void Postfix(Recipe __instance, int __state)
            {
                if (__state != -1)
                    __instance.m_minStationLevel = __state;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
        public static class Inventory_Changed_PatchCirclets
        {
            private static void Prefix(Inventory __instance)
            {
                PatchInventory(__instance);
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
        private static class Inventory_AddItem_ItemData_amount_x_y_PatchCircletItemDataOnLoad
        {
            [HarmonyPriority(Priority.First)]
            [HarmonyBefore("shudnal.ExtraSlots")]
            private static void Prefix(ItemDrop.ItemData item)
            {
                if (!getFeaturesByUpgrade.Value)
                    return;

                if (!IsCircletItemData(item))
                    return;

                PatchCircletItemData(item);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetAvailableRecipes))]
        public static class Player_GetAvailableRecipes_RemoveUncraftableRecipe
        {
            [HarmonyPriority(Priority.First)]
            private static void Postfix(Player __instance, ref List<Recipe> available)
            {
                if (!circletRecipeCraftingEnabled.Value)
                    available.RemoveAll(rec => rec == recipe);
            }
        }
    }
}
