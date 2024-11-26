using System.Linq;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using ServerSync;
using System;

namespace CircletExtended
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    [BepInIncompatibility("randyknapp.mods.dvergercolor")]
    [BepInIncompatibility("Azumatt.CircletDemister")]
    [BepInDependency("Azumatt.AzuExtendedPlayerInventory", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("shudnal.ExtraSlots", BepInDependency.DependencyFlags.SoftDependency)]
    public class CircletExtended : BaseUnityPlugin
    {
        const string pluginID = "shudnal.CircletExtended";
        const string pluginName = "Circlet Extended";
        const string pluginVersion = "1.0.18";

        private readonly Harmony harmony = new Harmony(pluginID);

        internal static readonly ConfigSync configSync = new ConfigSync(pluginID) { DisplayName = pluginName, CurrentVersion = pluginVersion, MinimumRequiredVersion = pluginVersion };

        public static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> configLocked;
        private static ConfigEntry<bool> loggingEnabled;

        public static ConfigEntry<bool> equipCircletUnderHelmet;
        public static ConfigEntry<string> equipCircletWithHelmet;

        public static ConfigEntry<int> itemSlotType;
        public static ConfigEntry<bool> itemSlotAzuEPI;
        public static ConfigEntry<string> itemSlotNameAzuEPI;
        public static ConfigEntry<int> itemSlotIndexAzuEPI;
        public static ConfigEntry<bool> itemSlotExtraSlots;
        public static ConfigEntry<string> itemSlotNameExtraSlots;
        public static ConfigEntry<int> itemSlotIndexExtraSlots;
        public static ConfigEntry<bool> itemSlotExtraSlotsDiscovery;

        public static ConfigEntry<bool> getFeaturesByUpgrade;
        public static ConfigEntry<int> overloadChargesPerLevel;
        public static ConfigEntry<bool> enableOverload;
        public static ConfigEntry<bool> enableDemister;
        public static ConfigEntry<bool> enablePutOnTop;

        public static ConfigEntry<int> fuelMinutes;
        public static ConfigEntry<int> fuelPerLevel;

        public static ConfigEntry<Color> circletColor;

        public static ConfigEntry<bool> disableOnSleep;
        public static ConfigEntry<bool> enableShadows;

        public static ConfigEntry<bool> enableOverloadDemister;
        public static ConfigEntry<float> overloadDemisterRange;
        public static ConfigEntry<float> overloadDemisterTime;

        public static ConfigEntry<bool> visualStateItemDrop;
        public static ConfigEntry<bool> visualStateItemStand;
        public static ConfigEntry<bool> visualStateArmorStand;

        public static ConfigEntry<string> circletRecipeQuality1;
        public static ConfigEntry<string> circletRecipeQuality2;
        public static ConfigEntry<string> circletRecipeQuality3;
        public static ConfigEntry<string> circletRecipeQuality4;

        public static ConfigEntry<KeyboardShortcut> widenShortcut;
        public static ConfigEntry<KeyboardShortcut> narrowShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleDemisterShortcut;

        public static ConfigEntry<KeyboardShortcut> increaseIntensityShortcut;
        public static ConfigEntry<KeyboardShortcut> decreaseIntensityShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleShadowsShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleSpotShortcut;

        public static ConfigEntry<KeyboardShortcut> overloadShortcut;

        public static ConfigEntry<int> maxSteps;
        public static ConfigEntry<float> minAngle;
        public static ConfigEntry<float> maxAngle;
        public static ConfigEntry<float> minIntensity;
        public static ConfigEntry<float> maxIntensity;
        public static ConfigEntry<float> minRange;
        public static ConfigEntry<float> maxRange;
        public static ConfigEntry<float> pointIntensity;
        public static ConfigEntry<float> pointRange;

        public static ConfigEntry<int> maxSteps2;
        public static ConfigEntry<float> minAngle2;
        public static ConfigEntry<float> maxAngle2;
        public static ConfigEntry<float> minIntensity2;
        public static ConfigEntry<float> maxIntensity2;
        public static ConfigEntry<float> minRange2;
        public static ConfigEntry<float> maxRange2;
        public static ConfigEntry<float> pointIntensity2;
        public static ConfigEntry<float> pointRange2;

        public static ConfigEntry<int> maxSteps3;
        public static ConfigEntry<float> minAngle3;
        public static ConfigEntry<float> maxAngle3;
        public static ConfigEntry<float> minIntensity3;
        public static ConfigEntry<float> maxIntensity3;
        public static ConfigEntry<float> minRange3;
        public static ConfigEntry<float> maxRange3;
        public static ConfigEntry<float> pointIntensity3;
        public static ConfigEntry<float> pointRange3;

        public static ConfigEntry<int> maxSteps4;
        public static ConfigEntry<float> minAngle4;
        public static ConfigEntry<float> maxAngle4;
        public static ConfigEntry<float> minIntensity4;
        public static ConfigEntry<float> maxIntensity4;
        public static ConfigEntry<float> minRange4;
        public static ConfigEntry<float> maxRange4;
        public static ConfigEntry<float> pointIntensity4;
        public static ConfigEntry<float> pointRange4;

        internal static CircletExtended instance;

        public static List<int> hotkeys = new List<int>();

        public static Dictionary<int, Piece.Requirement[]> recipeRequirements = new Dictionary<int, Piece.Requirement[]>();

        public static HashSet<int> equipWithHelmetsList = new HashSet<int>();

        public static string customDataKey = $"{pluginID}.DvergerLightState";
        public static string allHelmetsString = "AllHelmets";
        public static int allHelmetsHash = allHelmetsString.GetStableHashCode();

        private void Awake()
        {
            harmony.PatchAll();

            instance = this;

            ConfigInit();
            _ = configSync.AddLockingConfigEntry(configLocked);

            Game.isModded = true;

            if (itemSlotAzuEPI.Value && AzuExtendedPlayerInventory.API.IsLoaded())
                AzuExtendedPlayerInventory.API.AddSlot(itemSlotNameAzuEPI.Value, player => player.GetCirclet(), item => CircletItem.IsCircletItem(item), itemSlotIndexAzuEPI.Value);

            if (ExtraSlotsAPI.API.IsReady())
                if (itemSlotIndexExtraSlots.Value < 0)
                    ExtraSlotsAPI.API.AddSlotBefore("CircletExtended", () => itemSlotNameExtraSlots.Value, item => CircletItem.IsCircletItem(item), () => CircletItem.IsCircletSlotAvailable(), "HipLantern");
                else
                    ExtraSlotsAPI.API.AddSlotWithIndex("CircletExtended", itemSlotIndexExtraSlots.Value, () => itemSlotNameExtraSlots.Value, item => CircletItem.IsCircletItem(item), () => CircletItem.IsCircletSlotAvailable());
        }

        private void OnDestroy()
        {
            Config.Save();
            instance = null;
            harmony?.UnpatchSelf();
        }

        public static void LogInfo(object data)
        {
            if (loggingEnabled.Value)
                instance.Logger.LogInfo(data);
        }

        public void ConfigInit()
        {
            config("General", "NexusID", 2617, "Nexus mod ID for updates", false);

            modEnabled = config("General", "Enabled", defaultValue: true, "Enable the mod.");
            configLocked = config("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only.");
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging. [Not Synced with Server]", false);

            circletColor = config("Circlet", "Light color", defaultValue: new Color(1f, 0.9f, 0.75f, 1f), "Circlet beam light color. Changing this ingame will change current circlet color [Not Synced with Server]", false);
            disableOnSleep = config("Circlet", "Disable when sleeping", defaultValue: true, "Turn off the light when sleeping. [Not Synced with Server]", false);
            enableShadows = config("Circlet", "Enables shadows toggle", defaultValue: true, "Enables option to toggle circlet's light to emit shadows. Disable if it impacts your performance. [Not Synced with Server]", false);

            getFeaturesByUpgrade = config("Circlet - Features", "Get features by circlet upgrade", defaultValue: true, "Get circlet features by upgrading it. If False all features are available by default.\n" +
                                                                                                                        "If True the order of upgrades are \"Default\" -> \"Put on top\" -> \"Overload\" -> \"Demister\"");
            overloadChargesPerLevel = config("Circlet - Features", "Overload charges", 50, "How many overload charges is available. It is the fraction of durability being damaged on Overload use. x2 if upgrades are disabled or quality is maximum.");
            enableOverload = config("Circlet - Features", "Enable overload", defaultValue: true, "Enables overload. Press hotkey to blind opponents with a bright flash at the cost of some circlet durability");
            enableDemister = config("Circlet - Features", "Enable demister", defaultValue: true, "Enables demister. Press hotkey to spawn a little wisp to push away the mists");
            enablePutOnTop = config("Circlet - Features", "Enable put on top", defaultValue: true, "Enables equipping circlet on top of other helmet. Equip circlet without using a helmet slot.");

            getFeaturesByUpgrade.SettingChanged += (sender, args) => CircletItem.PatchCircletItemOnConfigChange();
            getFeaturesByUpgrade.SettingChanged += (sender, args) => CircletItem.FillRecipe();
            enablePutOnTop.SettingChanged += (sender, args) => CircletItem.PatchCircletItemOnConfigChange();

            fuelMinutes = config("Circlet - Fuel", "Basic fuel capacity", defaultValue: 360, "Time in minutes required to consume all fuel. Set to 0 to not consume fuel.");
            fuelPerLevel = config("Circlet - Fuel", "Fuel per level", defaultValue: 120, "Time in minutes added per quality level");

            fuelMinutes.SettingChanged += (sender, args) => CircletItem.PatchCircletItemOnConfigChange();
            fuelPerLevel.SettingChanged += (sender, args) => CircletItem.PatchCircletItemOnConfigChange();

            enableOverloadDemister = config("Circlet - Overload demister", "Enable temporary demister on overload", defaultValue: true, "Push away mist on overload activation");
            overloadDemisterRange = config("Circlet - Overload demister", "Range", defaultValue: 40f, "Maximum range");
            overloadDemisterTime = config("Circlet - Overload demister", "Time", defaultValue: 8f, "Time to gradually decrease effect radius");

            equipCircletUnderHelmet = config("Circlet - Put on top", "Equip under helmet", defaultValue: true, "If enabled - Circlet will be invisible if put on top of the helmet." +
                                                                                                               "\nIf disabled - Circlet will replace helmet");
            equipCircletWithHelmet = config("Circlet - Put on top", "Show when helmet equipped", defaultValue: "HelmetTrollLeather", "Comma separated list. If you have \"Equip under helmet\" enabled and wear a helmet from that list the Circlet will be shown." +
                                                                                                                                     "\nAdd identifier \"" + allHelmetsString + "\" to show circlet with every helmet equiped. Use that to test how it looks with different helmets." +
                                                                                                                                     "\nThere is only Troll Leather Helmet of Vanilla helmets that looks good with Circlet.");

            equipCircletWithHelmet.SettingChanged += (sender, args) => FillHelmets();

            circletRecipeQuality1 = config("Circlet - Recipe", "Create", defaultValue: "HelmetBronze:1,Ruby:1,SilverNecklace:1,SurtlingCore:10", "Recipe to create circet");
            circletRecipeQuality2 = config("Circlet - Recipe", "Upgrade quality 2", defaultValue: "Resin:20,LeatherScraps:10,IronNails:10,Chain:1", "Recipe to upgrade circet to quality 2");
            circletRecipeQuality3 = config("Circlet - Recipe", "Upgrade quality 3", defaultValue: "Thunderstone:5,Silver:1,JuteRed:2", "Recipe to upgrade circet to quality 3");
            circletRecipeQuality4 = config("Circlet - Recipe", "Upgrade quality 4", defaultValue: "Demister:1,BlackCore:1", "Recipe to upgrade circet to quality 4");

            circletRecipeQuality1.SettingChanged += (sender, args) => CircletItem.FillRecipe();
            circletRecipeQuality2.SettingChanged += (sender, args) => CircletItem.FillRecipe();
            circletRecipeQuality3.SettingChanged += (sender, args) => CircletItem.FillRecipe();
            circletRecipeQuality4.SettingChanged += (sender, args) => CircletItem.FillRecipe();

            visualStateItemDrop = config("Circlet - Visual state", "Enable itemdrop state", defaultValue: true, "Circlet dropped on the ground will preserve light state");
            visualStateItemStand = config("Circlet - Visual state", "Enable item stand state", defaultValue: true, "Circlet put on the item stand will preserve light state");
            visualStateArmorStand = config("Circlet - Visual state", "Enable armor stand state", defaultValue: true, "Circlet put on the armor stand will preserve light state");

            itemSlotType = config("Circlet - Custom slot", "Slot type", defaultValue: 55, "Custom item slot type. Change it only if you have issues with other mods compatibility. Game restart is recommended after change.");
            itemSlotAzuEPI = config("Circlet - Custom slot", "AzuEPI - Create slot", defaultValue: false, "Create custom equipment slot with AzuExtendedPlayerInventory. Game restart is required to apply changes.");
            itemSlotNameAzuEPI = config("Circlet - Custom slot", "AzuEPI - Slot name", defaultValue: "Circlet", "Custom equipment slot name. Game restart is required to apply changes.");
            itemSlotIndexAzuEPI = config("Circlet - Custom slot", "AzuEPI - Slot index", defaultValue: -1, "Slot index (position). Game restart is required to apply changes.");
            itemSlotExtraSlots = config("Circlet - Custom slot", "ExtraSlots - Create slot", defaultValue: false, "Create custom equipment slot with ExtraSlots.");
            itemSlotNameExtraSlots = config("Circlet - Custom slot", "ExtraSlots - Slot name", defaultValue: "Circlet", "Custom equipment slot name.");
            itemSlotIndexExtraSlots = config("Circlet - Custom slot", "ExtraSlots - Slot index", defaultValue: -1, "Slot index (position). Game restart is required to apply changes.");
            itemSlotExtraSlotsDiscovery = config("Circlet - Custom slot", "ExtraSlots - Available after discovery", defaultValue: true, "If enabled - slot will be active only if you know circlet item.");

            itemSlotType.SettingChanged += (sender, args) => CircletItem.PatchCircletItemOnConfigChange();
            itemSlotExtraSlots.SettingChanged += (s, e) => ExtraSlotsAPI.API.UpdateSlots();

            widenShortcut = config("Hotkeys", "Beam widen", defaultValue: new KeyboardShortcut(KeyCode.RightArrow), "Widen beam shortcut. [Not Synced with Server]", false);
            narrowShortcut = config("Hotkeys", "Beam narrow", defaultValue: new KeyboardShortcut(KeyCode.LeftArrow), "Narrow beam shortcut. [Not Synced with Server]", false);
            overloadShortcut = config("Hotkeys", "Overload", defaultValue: new KeyboardShortcut(KeyCode.T), "Overload shortcut. Blind opponents with a bright flash at the cost of some circlet durability. [Not Synced with Server]", false);
            toggleShortcut = config("Hotkeys", "Toggle light", defaultValue: new KeyboardShortcut(KeyCode.UpArrow), "Toggle main light shortcut. Enable/disable frontlight. [Not Synced with Server]", false);
            toggleDemisterShortcut = config("Hotkeys", "Toggle demister", defaultValue: new KeyboardShortcut(KeyCode.DownArrow), "Toggle demister shortcut. Spawn/despawn demister wisplight. [Not Synced with Server]", false);

            increaseIntensityShortcut = config("Hotkeys - Extra", "Intensity increase", defaultValue: new KeyboardShortcut(KeyCode.UpArrow, new KeyCode[1] { KeyCode.LeftShift }), "Increase intensity shortcut. Light becomes brighter and have more range. Intensity is capped at 150% [Not Synced with Server]", false);
            decreaseIntensityShortcut = config("Hotkeys - Extra", "Intensity decrease", defaultValue: new KeyboardShortcut(KeyCode.DownArrow, new KeyCode[1] { KeyCode.LeftShift }), "Decrease intensity shortcut. Light becomes darker and have less range. Intensity is capped at 50% [Not Synced with Server]", false);
            toggleShadowsShortcut = config("Hotkeys - Extra", "Toggle shadows", defaultValue: new KeyboardShortcut(KeyCode.LeftArrow, new KeyCode[1] { KeyCode.LeftShift }), "Toggle shadows shortcut. Enables/disables the current light source to emit soft shadows. [Not Synced with Server]", false);
            toggleSpotShortcut = config("Hotkeys - Extra", "Toggle radiance", defaultValue: new KeyboardShortcut(KeyCode.RightArrow, new KeyCode[1] { KeyCode.LeftShift }), "Toggle spotlight shortcut. Enables/disables the radiance when circlet is equipped. [Not Synced with Server]", false);

            widenShortcut.SettingChanged += (sender, args) => FillShortcuts();
            narrowShortcut.SettingChanged += (sender, args) => FillShortcuts();
            overloadShortcut.SettingChanged += (sender, args) => FillShortcuts();
            toggleShortcut.SettingChanged += (sender, args) => FillShortcuts();
            toggleDemisterShortcut.SettingChanged += (sender, args) => FillShortcuts();
            increaseIntensityShortcut.SettingChanged += (sender, args) => FillShortcuts();
            decreaseIntensityShortcut.SettingChanged += (sender, args) => FillShortcuts();
            toggleShadowsShortcut.SettingChanged += (sender, args) => FillShortcuts();
            toggleSpotShortcut.SettingChanged += (sender, args) => FillShortcuts();

            maxSteps = config("Light - Default", "Max Steps", 3, "Define how many steps of focus the Dverger light beam has. Must be at least 2.");
            minAngle = config("Light - Default", "Min Angle", 30.0f, "The angle of the beam at the narrowest setting.");
            maxAngle = config("Light - Default", "Max Angle", 110.0f, "The angle of the beam at the widest setting.");
            minIntensity = config("Light - Default", "Min Intensity", 1.4f, "The intensity of the beam at the widest setting.");
            maxIntensity = config("Light - Default", "Max Intensity", 2.2f, "The intensity of the beam at the narrowest setting");
            minRange = config("Light - Default", "Min Range", 45.0f, "The range of the beam at the narrowest setting.");
            maxRange = config("Light - Default", "Max Range", 15.0f, "The range of the beam at the widest setting");
            pointIntensity = config("Light - Default", "Point Intensity", 1.1f, "The intensity of the Dverger light pool on the point light setting.");
            pointRange = config("Light - Default", "Point Range", 10.0f, "The range of the Dverger light pool on the point light setting.");

            maxSteps2 = config("Light - Quality 2", "Max Steps", 3, "Define how many steps of focus the Dverger light beam has. Must be at least 2.");
            minAngle2 = config("Light - Quality 2", "Min Angle", 30.0f, "The angle of the beam at the narrowest setting.");
            maxAngle2 = config("Light - Quality 2", "Max Angle", 110.0f, "The angle of the beam at the widest setting.");
            minIntensity2 = config("Light - Quality 2", "Min Intensity", 1.4f, "The intensity of the beam at the widest setting.");
            maxIntensity2 = config("Light - Quality 2", "Max Intensity", 2.2f, "The intensity of the beam at the narrowest setting");
            minRange2 = config("Light - Quality 2", "Min Range", 45.0f, "The range of the beam at the narrowest setting.");
            maxRange2 = config("Light - Quality 2", "Max Range", 15.0f, "The range of the beam at the widest setting");
            pointIntensity2 = config("Light - Quality 2", "Point Intensity", 1.1f, "The intensity of the Dverger light pool on the point light setting.");
            pointRange2 = config("Light - Quality 2", "Point Range", 10.0f, "The range of the Dverger light pool on the point light setting.");

            maxSteps3 = config("Light - Quality 3", "Max Steps", 3, "Define how many steps of focus the Dverger light beam has. Must be at least 2.");
            minAngle3 = config("Light - Quality 3", "Min Angle", 30.0f, "The angle of the beam at the narrowest setting.");
            maxAngle3 = config("Light - Quality 3", "Max Angle", 110.0f, "The angle of the beam at the widest setting.");
            minIntensity3 = config("Light - Quality 3", "Min Intensity", 1.4f, "The intensity of the beam at the widest setting.");
            maxIntensity3 = config("Light - Quality 3", "Max Intensity", 2.2f, "The intensity of the beam at the narrowest setting");
            minRange3 = config("Light - Quality 3", "Min Range", 45.0f, "The range of the beam at the narrowest setting.");
            maxRange3 = config("Light - Quality 3", "Max Range", 15.0f, "The range of the beam at the widest setting");
            pointIntensity3 = config("Light - Quality 3", "Point Intensity", 1.1f, "The intensity of the Dverger light pool on the point light setting.");
            pointRange3 = config("Light - Quality 3", "Point Range", 10.0f, "The range of the Dverger light pool on the point light setting.");

            maxSteps4 = config("Light - Quality 4", "Max Steps", 3, "Define how many steps of focus the Dverger light beam has. Must be at least 2.");
            minAngle4 = config("Light - Quality 4", "Min Angle", 30.0f, "The angle of the beam at the narrowest setting.");
            maxAngle4 = config("Light - Quality 4", "Max Angle", 110.0f, "The angle of the beam at the widest setting.");
            minIntensity4 = config("Light - Quality 4", "Min Intensity", 1.4f, "The intensity of the beam at the widest setting.");
            maxIntensity4 = config("Light - Quality 4", "Max Intensity", 2.2f, "The intensity of the beam at the narrowest setting");
            minRange4 = config("Light - Quality 4", "Min Range", 45.0f, "The range of the beam at the narrowest setting.");
            maxRange4 = config("Light - Quality 4", "Max Range", 15.0f, "The range of the beam at the widest setting");
            pointIntensity4 = config("Light - Quality 4", "Point Intensity", 1.1f, "The intensity of the Dverger light pool on the point light setting.");
            pointRange4 = config("Light - Quality 4", "Point Range", 10.0f, "The range of the Dverger light pool on the point light setting.");

            FillShortcuts();

            FillHelmets();
        }

        private void FillShortcuts()
        {
            Dictionary<int, int> shortcutsModifiers = new Dictionary<int, int>();

            AddShortcut(shortcutsModifiers, widenShortcut);
            AddShortcut(shortcutsModifiers, narrowShortcut);
            AddShortcut(shortcutsModifiers, toggleShortcut);
            AddShortcut(shortcutsModifiers, toggleSpotShortcut);
            AddShortcut(shortcutsModifiers, increaseIntensityShortcut);
            AddShortcut(shortcutsModifiers, decreaseIntensityShortcut);
            AddShortcut(shortcutsModifiers, toggleShadowsShortcut);
            AddShortcut(shortcutsModifiers, overloadShortcut);
            AddShortcut(shortcutsModifiers, toggleDemisterShortcut);

            hotkeys = shortcutsModifiers.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
        }

        private void FillHelmets()
        {
            equipWithHelmetsList = new HashSet<int>(equipCircletWithHelmet.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(c => { return c.Trim().GetStableHashCode(); }));
        }

        private void AddShortcut(Dictionary<int, int> shortcuts, ConfigEntry<KeyboardShortcut> shortcut)
        {
            shortcuts.Add(shortcut.Definition.GetHashCode(), shortcut.Value.Modifiers.Count());
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, defaultValue, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, string description, bool synchronizedSetting = true) => config(group, name, defaultValue, new ConfigDescription(description), synchronizedSetting);

        public static int GetMaxSteps(int quality)
        {
            int targetQuality = getFeaturesByUpgrade.Value ? Mathf.Clamp(quality, 1, 4) : 1;

            switch (targetQuality)
            {
                case 1:
                    return maxSteps.Value;
                case 2:
                    return maxSteps2.Value;
                case 3:
                    return maxSteps3.Value;
                case 4:
                    return maxSteps4.Value;
            };

            return maxSteps.Value;
        }

        public static float GetMinAngle(int quality)
        {
            int targetQuality = getFeaturesByUpgrade.Value ? Mathf.Clamp(quality, 1, 4) : 1;

            switch (targetQuality)
            {
                case 1:
                    return minAngle.Value;
                case 2:
                    return minAngle2.Value;
                case 3:
                    return minAngle3.Value;
                case 4:
                    return minAngle4.Value;
            };

            return minAngle.Value;
        }

        public static float GetMaxAngle(int quality)
        {
            int targetQuality = getFeaturesByUpgrade.Value ? Mathf.Clamp(quality, 1, 4) : 1;

            switch (targetQuality)
            {
                case 1:
                    return maxAngle.Value;
                case 2:
                    return maxAngle2.Value;
                case 3:
                    return maxAngle3.Value;
                case 4:
                    return maxAngle4.Value;
            };

            return maxAngle.Value;
        }

        public static float GetMinIntensity(int quality)
        {
            int targetQuality = getFeaturesByUpgrade.Value ? Mathf.Clamp(quality, 1, 4) : 1;

            switch (targetQuality)
            {
                case 1:
                    return minIntensity.Value;
                case 2:
                    return minIntensity2.Value;
                case 3:
                    return minIntensity3.Value;
                case 4:
                    return minIntensity4.Value;
            };

            return minIntensity.Value;
        }

        public static float GetMaxIntensity(int quality)
        {
            int targetQuality = getFeaturesByUpgrade.Value ? Mathf.Clamp(quality, 1, 4) : 1;

            switch (targetQuality)
            {
                case 1:
                    return maxIntensity.Value;
                case 2:
                    return maxIntensity2.Value;
                case 3:
                    return maxIntensity3.Value;
                case 4:
                    return maxIntensity4.Value;
            };

            return maxIntensity.Value;
        }

        public static float GetMinRange(int quality)
        {
            int targetQuality = getFeaturesByUpgrade.Value ? Mathf.Clamp(quality, 1, 4) : 1;

            switch (targetQuality)
            {
                case 1:
                    return minRange.Value;
                case 2:
                    return minRange2.Value;
                case 3:
                    return minRange3.Value;
                case 4:
                    return minRange4.Value;
            };

            return minRange.Value;
        }

        public static float GetMaxRange(int quality)
        {
            int targetQuality = getFeaturesByUpgrade.Value ? Mathf.Clamp(quality, 1, 4) : 1;

            switch (targetQuality)
            {
                case 1:
                    return maxRange.Value;
                case 2:
                    return maxRange2.Value;
                case 3:
                    return maxRange3.Value;
                case 4:
                    return maxRange4.Value;
            };

            return maxRange.Value;
        }

        public static float GetPointIntensity(int quality)
        {
            int targetQuality = getFeaturesByUpgrade.Value ? Mathf.Clamp(quality, 1, 4) : 1;

            switch (targetQuality)
            {
                case 1:
                    return pointIntensity.Value;
                case 2:
                    return pointIntensity2.Value;
                case 3:
                    return pointIntensity3.Value;
                case 4:
                    return pointIntensity4.Value;
            };

            return pointIntensity.Value;
        }

        public static float GetPointRange(int quality)
        {
            int targetQuality = getFeaturesByUpgrade.Value ? Mathf.Clamp(quality, 1, 4) : 1;

            switch (targetQuality)
            {
                case 1:
                    return pointRange.Value;
                case 2:
                    return pointRange2.Value;
                case 3:
                    return pointRange3.Value;
                case 4:
                    return pointRange4.Value;
            };

            return pointRange.Value;
        }
    }
}