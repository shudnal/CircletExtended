using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly HashSet<Piece.Requirement> configuredRecipeRequirements = new HashSet<Piece.Requirement>();

        private struct RecipeRequirementsPatchState
        {
            public Recipe Recipe;
            public Piece.Requirement[] Resources;
        }

        private struct RecipeStationLevelPatchState
        {
            public Recipe Recipe;
            public int MinStationLevel;
        }

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

        internal static bool IsCircletItem(int hash)
        {
            return hash == itemHashHelmetDverger;
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
            recipe.m_minStationLevel = circletRecipeCraftingStationLvl.Value;
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
            configuredRecipeRequirements.Clear();

            for (int quality = 0; quality <= 5; quality++)
            {
                Piece.Requirement[] requirements = GetRequirements(quality);
                recipeRequirements.Add(quality, requirements);
                configuredRecipeRequirements.UnionWith(requirements);
            }
        }

        private static bool TryApplyRecipeRequirements(Recipe targetRecipe, int quality, out RecipeRequirementsPatchState state)
        {
            state = default;

            if (!getFeaturesByUpgrade.Value || targetRecipe != recipe || quality <= 1)
                return false;

            if (!recipeRequirements.TryGetValue(quality, out Piece.Requirement[] requirements))
                return false;

            state.Recipe = targetRecipe;
            state.Resources = targetRecipe.m_resources;
            targetRecipe.m_resources = requirements;
            return true;
        }

        private static void RestoreRecipeRequirements(RecipeRequirementsPatchState state)
        {
            if (state.Recipe != null)
                state.Recipe.m_resources = state.Resources;
        }

        private static void ApplyRepairStationLevel(ItemDrop.ItemData item, out RecipeStationLevelPatchState state)
        {
            state = default;

            if (recipe == null || !IsCircletItemData(item))
                return;

            state.Recipe = recipe;
            state.MinStationLevel = recipe.m_minStationLevel;
            recipe.m_minStationLevel = circletRecipeRepairStationLvl.Value;
        }

        private static void RestoreStationLevel(RecipeStationLevelPatchState state)
        {
            if (state.Recipe != null)
                state.Recipe.m_minStationLevel = state.MinStationLevel;
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

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load), new[] { typeof(ZPackage), typeof(bool) } )]
        public static class Inventory_Load_New_CircletStats
        {
            public static void Postfix(Inventory __instance)
            {
                if (__instance.m_temoraryInventory)
                    return;

                PatchInventory(__instance);
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load), new[] { typeof(ZPackage) })]
        public static class Inventory_Load_CircletStats
        {
            public static void Postfix(Inventory __instance)
            {
                if (__instance.m_temoraryInventory)
                    return;

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
                {
                    __result = qualityLevel > 1 ? 0 : 1;
                    return;
                }

                if (configuredRecipeRequirements.Contains(__instance))
                    __result = __instance.m_amount;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        public static class InventoryGui_DoCrafting_CircletUpgrade
        {
            private static bool PatchMethod(ItemDrop.ItemData craftUpgradeItem, Recipe craftRecipe)
            {
                return getFeaturesByUpgrade.Value && craftRecipe == recipe && craftUpgradeItem != null && IsCircletItemData(craftUpgradeItem);
            }

            [HarmonyPriority(Priority.First)]
            private static void Prefix(Recipe ___m_craftRecipe, ItemDrop.ItemData ___m_craftUpgradeItem, out RecipeRequirementsPatchState __state)
            {
                __state = default;

                if (!PatchMethod(___m_craftUpgradeItem, ___m_craftRecipe))
                    return;

                TryApplyRecipeRequirements(___m_craftRecipe, ___m_craftUpgradeItem.m_quality + 1, out __state);
            }

            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Recipe ___m_craftRecipe, ItemDrop.ItemData ___m_craftUpgradeItem, RecipeRequirementsPatchState __state)
            {
                if (!PatchMethod(___m_craftUpgradeItem, ___m_craftRecipe))
                    return;

                RestoreRecipeRequirements(__state);
                PatchCircletItemData(___m_craftUpgradeItem, inventoryItemUpdate: false);
            }

            [HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception, RecipeRequirementsPatchState __state)
            {
                if (__exception != null)
                    RestoreRecipeRequirements(__state);

                return __exception;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirementList))]
        public static class InventoryGui_SetupRequirementList_CircletUpgrade
        {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(int quality, InventoryGui.RecipeDataPair ___m_selectedRecipe, out RecipeRequirementsPatchState __state)
            {
                __state = default;

                if (___m_selectedRecipe.Recipe != recipe)
                    return;

                TryApplyRecipeRequirements(___m_selectedRecipe.Recipe, quality, out __state);
            }

            [HarmonyPriority(Priority.Last)]
            private static void Postfix(RecipeRequirementsPatchState __state)
            {
                RestoreRecipeRequirements(__state);
            }

            [HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception, RecipeRequirementsPatchState __state)
            {
                if (__exception != null)
                    RestoreRecipeRequirements(__state);

                return __exception;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
        public static class InventoryGui_SetupRequirement_CircletUnknownRequirement
        {
            private static void Postfix(bool __result, Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality)
            {
                if (!__result || !craft || quality <= 1 || !getFeaturesByUpgrade.Value)
                    return;

                if (req == null || !configuredRecipeRequirements.Contains(req) || !req.m_resItem)
                    return;

                if (player.IsKnownMaterial(req.m_resItem.m_itemData.m_shared.m_name))
                    return;

                UnityEngine.UI.Image icon = elementRoot.Find("res_icon")?.GetComponent<UnityEngine.UI.Image>();
                TMPro.TMP_Text name = elementRoot.Find("res_name")?.GetComponent<TMPro.TMP_Text>();
                TMPro.TMP_Text amount = elementRoot.Find("res_amount")?.GetComponent<TMPro.TMP_Text>();
                UITooltip tooltip = elementRoot.GetComponent<UITooltip>();

                if (icon != null)
                    icon.color = circletUnknownRequirementIconColor.Value;

                if (name != null)
                    name.text = "???";

                if (amount != null)
                {
                    amount.text = "???";
                    amount.color = Color.white;
                }

                if (tooltip != null)
                    tooltip.m_text = "";
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
        public static class Player_HaveRequirementItems_CircletUpgrade
        {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Recipe piece, bool discover, int qualityLevel, out RecipeRequirementsPatchState __state)
            {
                __state = default;

                if (discover)
                    return;

                TryApplyRecipeRequirements(piece, qualityLevel, out __state);
            }

            [HarmonyPriority(Priority.Last)]
            private static void Postfix(RecipeRequirementsPatchState __state)
            {
                RestoreRecipeRequirements(__state);
            }

            [HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception, RecipeRequirementsPatchState __state)
            {
                if (__exception != null)
                    RestoreRecipeRequirements(__state);

                return __exception;
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

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
        private static class ItemDropItemData_GetTooltip_ItemTooltip
        {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(ItemDrop.ItemData item, out RecipeStationLevelPatchState __state)
            {
                ApplyRepairStationLevel(item, out __state);
            }

            [HarmonyPriority(Priority.Last)]
            private static void Postfix(ItemDrop.ItemData item, RecipeStationLevelPatchState __state, ref string __result)
            {
                RestoreStationLevel(__state);

                if (IsCircletItemData(item) && UseFuel())
                    __result = __result.Replace("$item_durability", "$piece_fire_fuel");
            }

            [HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception, RecipeStationLevelPatchState __state)
            {
                if (__exception != null)
                    RestoreStationLevel(__state);

                return __exception;
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

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.CanRepair))]
        public static class InventoryGui_CanRepair_CircletRepairStationLevel
        {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(ItemDrop.ItemData item, out RecipeStationLevelPatchState __state)
            {
                ApplyRepairStationLevel(item, out __state);
            }

            [HarmonyPriority(Priority.Last)]
            private static void Postfix(RecipeStationLevelPatchState __state)
            {
                RestoreStationLevel(__state);
            }

            [HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception, RecipeStationLevelPatchState __state)
            {
                if (__exception != null)
                    RestoreStationLevel(__state);

                return __exception;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
        public static class Inventory_Changed_PatchCirclets
        {
            private static void Prefix(Inventory __instance)
            {
                if (__instance.m_temoraryInventory)
                    return;

                PatchInventory(__instance);
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool))]
        private static class Inventory_AddItem_ItemData_amount_x_y_PatchCircletItemDataOnLoad
        {
            [HarmonyPriority(Priority.First)]
            [HarmonyBefore("shudnal.ExtraSlots")]
            private static void Prefix(Inventory __instance, ItemDrop.ItemData item)
            {
                if (__instance.m_temoraryInventory)
                    return;

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
