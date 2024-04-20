﻿using HarmonyLib;
using static CircletExtended.CircletExtended;
using System.Runtime.CompilerServices;
using System;
using System.Linq;

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
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
    public static class Humanoid_SetupVisEquipment_CircletOnTop
    {
        private static void Postfix(Humanoid __instance, VisEquipment visEq)
        {
            if (!modEnabled.Value)
                return;

            if (!enablePutOnTop.Value)
                return;

            ItemDrop.ItemData circletHelmet = __instance.GetCirclet();

            string circletName = circletHelmet == null ? (__instance.m_helmetItem != null ? __instance.m_helmetItem.m_dropPrefab.name : "") : circletHelmet.m_dropPrefab.name;
            visEq.SetHelmetItem(circletName);
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
            
            if (item.m_shared.m_itemType == CircletItem.GetItemType())
            {
                bool wasCirclet = __instance.GetCirclet() != null;

                __instance.UnequipItem(__instance.GetCirclet(), triggerEquipEffects);

                if (wasCirclet)
                    __instance.m_visEquipment.UpdateEquipmentVisuals();

                __instance.SetCirclet(item);
            }
            
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
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            if (!modEnabled.Value)
                return;

            if (!enablePutOnTop.Value)
                return;

            if (item == null)
                return;

            if (__instance.GetCirclet() == item || __instance.m_helmetItem == null)
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
