using HarmonyLib;
using static CircletExtended.CircletExtended;
using System.Runtime.CompilerServices;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace CircletExtended
{
    [Serializable]
    public class HumanoidHelmetCirclet
    {
        public ItemDrop.ItemData circlet;

        public HumanoidHelmetCirclet()
        {
            circlet = null;
        }
    }

    public static class HumanoidExtension
    {
        private static readonly ConditionalWeakTable<Humanoid, HumanoidHelmetCirclet> data = new ConditionalWeakTable<Humanoid, HumanoidHelmetCirclet>();

        public static HumanoidHelmetCirclet GetCircletData(this Humanoid humanoid) => data.GetOrCreateValue(humanoid);

        public static ItemDrop.ItemData GetCirclet(this Humanoid humanoid) => humanoid.GetCircletData().circlet;

        public static ItemDrop.ItemData SetCirclet(this Humanoid humanoid, ItemDrop.ItemData item) => humanoid.GetCircletData().circlet = item;
        
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
        public static class Humanoid_SetupVisEquipment_CustomItemType
        {
            private static void Postfix(Humanoid __instance, VisEquipment visEq)
            {
                if (!modEnabled.Value)
                    return;

                if (!enablePutOnTop.Value)
                    return;

                ItemDrop.ItemData itemData = __instance.GetCirclet();

                visEq.SetCircletItem((itemData != null) ? itemData.m_dropPrefab.name : "");
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetEquipmentWeight))]
        public static class Humanoid_GetEquipmentWeight_CustomItemType
        {
            private static void Postfix(Humanoid __instance, ref float __result)
            {
                if (!modEnabled.Value)
                    return;

                if (!enablePutOnTop.Value)
                    return;

                ItemDrop.ItemData itemData = __instance.GetCirclet();
                if (itemData != null)
                    __result += itemData.m_shared.m_weight;
            }
        }
    }

    [Serializable]
    public class VisEquipmentCirclet
    {
        public string m_circletItem = "";
        public GameObject m_circletItemInstance;
        public int m_currentCircletItemHash = 0;

        public static readonly int s_circletItem = "CircletItem".GetStableHashCode();
    }

    public static class VisEquipmentExtension
    {
        private static readonly ConditionalWeakTable<VisEquipment, VisEquipmentCirclet> data = new ConditionalWeakTable<VisEquipment, VisEquipmentCirclet>();

        public static VisEquipmentCirclet GetCircletData(this VisEquipment visEquipment) => data.GetOrCreateValue(visEquipment);

        public static void SetCircletItem(this VisEquipment visEquipment, string name)
        {
            VisEquipmentCirclet circletData = visEquipment.GetCircletData();

            if (!(circletData.m_circletItem == name))
            {
                circletData.m_circletItem = name;
                if (visEquipment.m_nview.GetZDO() != null && visEquipment.m_nview.IsOwner())
                    visEquipment.m_nview.GetZDO().Set(VisEquipmentCirclet.s_circletItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
            }
        }

        public static bool SetCircletEquipped(this VisEquipment visEquipment, int hash)
        {
            VisEquipmentCirclet circletData = visEquipment.GetCircletData();
            if (circletData.m_currentCircletItemHash == hash)
            {
                return false;
            }

            if ((bool)circletData.m_circletItemInstance)
            {
                UnityEngine.Object.Destroy(circletData.m_circletItemInstance);
                circletData.m_circletItemInstance = null;
            }

            circletData.m_currentCircletItemHash = hash;
            if (hash != 0)
            {
                circletData.m_circletItemInstance = visEquipment.AttachItem(hash, 0, visEquipment.m_helmet);
            }

            return true;
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
        public static class VisEquipment_UpdateEquipmentVisuals_CustomItemType
        {
            private static void Prefix(VisEquipment __instance)
            {
                int circletEquipped = 0;
                ZDO zDO = __instance.m_nview.GetZDO();
                if (zDO != null)
                {
                    circletEquipped = zDO.GetInt(VisEquipmentCirclet.s_circletItem);
                }
                else
                {
                    VisEquipmentCirclet circletData = __instance.GetCircletData();
                    if (!string.IsNullOrEmpty(circletData.m_circletItem))
                    {
                        circletEquipped = circletData.m_circletItem.GetStableHashCode();
                    }
                }

                if (__instance.SetCircletEquipped(circletEquipped))
                    __instance.UpdateLodgroup();
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    public static class Humanoid_EquipItem_CircletOnTop
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result, bool triggerEquipEffects)
        {
            if (!modEnabled.Value)
                return;

            if (!enablePutOnTop.Value)
                return;

            if (__instance.m_helmetItem != null && __instance.m_helmetItem.m_shared.m_name == CircletItem.itemDropNameHelmetDverger)
            {
                LogInfo("Unequipping circlet on circlet equipment");
                __instance.UnequipItem(__instance.GetCirclet(), triggerEquipEffects);
                return;
            }

            if (item.m_shared.m_itemType != CircletItem.GetItemType())
                return;

            bool wasCirclet = __instance.GetCirclet() != null;

            __instance.UnequipItem(__instance.GetCirclet(), triggerEquipEffects);

            if (wasCirclet)
                __instance.m_visEquipment.UpdateEquipmentVisuals();

            __instance.SetCirclet(item);
            
            if (__instance.IsItemEquiped(item))
            {
                item.m_equipped = true;
                __result = true;
            }

            __instance.SetupEquipment();

            if (triggerEquipEffects)
                __instance.TriggerEquipEffect(item);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    public static class Humanoid_UnequipItem_CircletOnTop
    {
        private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __state) => __state = __instance.m_helmetItem == item;

        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, bool __state)
        {
            if (!modEnabled.Value)
                return;

            if (!enablePutOnTop.Value)
                return;

            if (item == null)
                return;

            if (__state)
                __instance.UnequipItem(__instance.GetCirclet());

            if (__instance.GetCirclet() != item)
                return;

            __instance.SetCirclet(null);

            __instance.SetupEquipment();

            if (triggerEquipEffects)
                __instance.TriggerEquipEffect(item);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
    public class Humanoid_UnequipAllItems_CircletOnTop
    {
        public static void Postfix(Humanoid __instance)
        {
            if (!modEnabled.Value)
                return;

            if (!enablePutOnTop.Value)
                return;

            __instance.UnequipItem(__instance.GetCirclet(), triggerEquipEffects: false); 
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
    public static class Humanoid_IsItemEquiped_CircletOnTop
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!modEnabled.Value)
                return;

            if (!enablePutOnTop.Value)
                return;

            if (item == null)
                return;

            __result = __result || __instance.GetCirclet() == item;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsEquipable))]
    public static class ItemDropItemData_IsEquipable_CircletOnTop
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref bool __result)
        {
            if (!modEnabled.Value)
                return;

            if (!enablePutOnTop.Value)
                return;

            __result = __result || __instance.m_shared.m_itemType == CircletItem.GetItemType();
        }
    }

    [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.Awake))]
    public static class ItemStand_Awake_CircletOnTop
    {
        private static void Postfix(ItemStand __instance)
        {
            if (!modEnabled.Value)
                return;

            if (!enablePutOnTop.Value)
                return;

            if (!visualStateItemStand.Value)
                return;

            __instance.m_supportedTypes.Add(CircletItem.GetItemType());
        }
    }

    [HarmonyPatch(typeof(ArmorStand), nameof(ArmorStand.Awake))]
    public static class ArmorStand_Awake_CircletOnTop
    {
        private static void Postfix(ArmorStand __instance)
        {
            if (!modEnabled.Value)
                return;

            if (!enablePutOnTop.Value)
                return;

            if (!visualStateArmorStand.Value)
                return;

            __instance.m_slots.Where(x => x.m_slot == VisSlot.Helmet).Do(x => x.m_supportedTypes.Add(CircletItem.GetItemType()));
        }
    }

}
