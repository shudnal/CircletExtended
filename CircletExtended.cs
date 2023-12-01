using System.Linq;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using ServerSync;

namespace CircletExtended
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    public class CircletExtended : BaseUnityPlugin
    {
        const string pluginID = "shudnal.CircletExtended";
        const string pluginName = "Circlet Extended";
        const string pluginVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(pluginID);

        internal static readonly ConfigSync configSync = new ConfigSync(pluginID) { DisplayName = pluginName, CurrentVersion = pluginVersion, MinimumRequiredVersion = pluginVersion };

        public static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> configLocked;
        private static ConfigEntry<bool> loggingEnabled;

        public static ConfigEntry<bool> getFeaturesByUpgrade;
        private static ConfigEntry<float> durabilityPerLevel;
        public static ConfigEntry<bool> enableOverload;
        public static ConfigEntry<bool> enableDemister;
        public static ConfigEntry<bool> enablePutOnTop;
        public static ConfigEntry<float> demisterRadius; 

        public static ConfigEntry<Color> circletColor;

        public static ConfigEntry<bool> disableOnSleep;
        public static ConfigEntry<bool> enableShadows;

        public static ConfigEntry<KeyboardShortcut> widenShortcut;
        public static ConfigEntry<KeyboardShortcut> narrowShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleDemisterShortcut;

        public static ConfigEntry<KeyboardShortcut> increaseIntensityShortcut;
        public static ConfigEntry<KeyboardShortcut> decreaseIntensityShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleShadowsShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleSpotShortcut;

        public static ConfigEntry<KeyboardShortcut> overloadShortcut;

        internal static CircletExtended instance;

        public const string itemNameHelmetDverger = "HelmetDverger";
        public const string itemDropNameHelmetDverger = "$item_helmet_dverger";
        public static int itemHashHelmetDverger = 703889544;
        public static GameObject overloadEffect;
        public static int demisterEffectHash = 0;

        public const int maxQuality = 4;

        public static List<int> hotkeys = new List<int>();

        public static Dictionary<int, Piece.Requirement[]> recipeRequirements = new Dictionary<int, Piece.Requirement[]>();

        /// <summary>
        /// //////
        /// </summary>
        ///        
        public static ConfigEntry<int> MaxSteps;
        public static ConfigEntry<float> MinAngle;
        public static ConfigEntry<float> MaxAngle;
        public static ConfigEntry<float> MinIntensity;
        public static ConfigEntry<float> MaxIntensity;
        public static ConfigEntry<float> MinRange;
        public static ConfigEntry<float> MaxRange;
        public static ConfigEntry<float> PointIntensity;
        public static ConfigEntry<float> PointRange;

        public static string customDataKey;

        private void Awake()
        {
            harmony.PatchAll();

            instance = this;

            ConfigInit();
            _ = configSync.AddLockingConfigEntry(configLocked);

            Game.isModded = true;

            customDataKey = $"{pluginID}.DvergerLightState";
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
            config("General", "NexusID", 0, "Nexus mod ID for updates", false);

            modEnabled = config("General", "Enabled", defaultValue: true, "Enable the mod.");
            configLocked = config("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only.");
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging. [Not Synced with Server]", false);

            circletColor = config("Circlet", "Light color", defaultValue: Color.white, "Circlet beam light color. Changing this ingame will change current circlet color [Not Synced with Server]", false);
            disableOnSleep = config("Circlet", "Disable when sleeping", defaultValue: true, "Turn off the light when sleeping. [Not Synced with Server]", false);
            enableShadows = config("Circlet", "Enables shadows toggle", defaultValue: true, "Enables option to toggle shadows. May impact the performance. [Not Synced with Server]", false);

            getFeaturesByUpgrade = config("Circlet - Features", "Get features by circlet upgrade", defaultValue: true, "Get circlet features by upgrading it. If not set all features are available by default.\n" +
                                                                                                                        "If set the order of upgrades are \"Default\" -> \"Put on top\" -> \"Overload\" -> \"Demister\"");
            durabilityPerLevel = config("Circlet - Features", "Durability per level", defaultValue: 500f, "Durability added per level");
            enableOverload = config("Circlet - Features", "Enable overload", defaultValue: true, "Enables overload. Press hotkey to blind opponents with a bright flash at the cost of some circlet durability");
            enableDemister = config("Circlet - Features", "Enable demister", defaultValue: true, "Enables demister. Spawn a little wisp to push away the mists");
            enablePutOnTop = config("Circlet - Features", "Enable put on top", defaultValue: true, "Enables put on top. Equip circlet without using a helmet slot.");
            demisterRadius = config("Circlet - Features", "Demister radius", defaultValue: 6f, "Demister effect radius. Default wisp radius is 6.");

            widenShortcut = config("Hotkeys", "Beam widen", defaultValue: new KeyboardShortcut(KeyCode.RightArrow), "Widen beam shortcut. [Not Synced with Server]", false);
            narrowShortcut = config("Hotkeys", "Beam narrow", defaultValue: new KeyboardShortcut(KeyCode.LeftArrow), "Narrow beam shortcut. [Not Synced with Server]", false);
            overloadShortcut = config("Hotkeys", "Overload", defaultValue: new KeyboardShortcut(KeyCode.T), "Overload shortcut. [Not Synced with Server]", false);
            toggleShortcut = config("Hotkeys", "Toggle light", defaultValue: new KeyboardShortcut(KeyCode.UpArrow), "Toggle main light shortcut. [Not Synced with Server]", false);
            toggleDemisterShortcut = config("Hotkeys", "Toggle demister", defaultValue: new KeyboardShortcut(KeyCode.DownArrow), "Toggle demister shortcut. [Not Synced with Server]", false);

            increaseIntensityShortcut = config("Hotkeys - Extra", "Intensity increase", defaultValue: new KeyboardShortcut(KeyCode.UpArrow, new KeyCode[1] { KeyCode.LeftShift }), "Increase intensity shortcut. [Not Synced with Server]", false);
            decreaseIntensityShortcut = config("Hotkeys - Extra", "Intensity decrease", defaultValue: new KeyboardShortcut(KeyCode.DownArrow, new KeyCode[1] { KeyCode.LeftShift }), "Decrease intensity shortcut. [Not Synced with Server]", false);
            toggleShadowsShortcut = config("Hotkeys - Extra", "Toggle shadows", defaultValue: new KeyboardShortcut(KeyCode.LeftArrow, new KeyCode[1] { KeyCode.LeftShift }), "Toggle shadows shortcut. [Not Synced with Server]", false);
            toggleSpotShortcut = config("Hotkeys - Extra", "Toggle radiance", defaultValue: new KeyboardShortcut(KeyCode.RightArrow, new KeyCode[1] { KeyCode.LeftShift }), "Toggle spotlight shortcut. [Not Synced with Server]", false);

            itemHashHelmetDverger = itemNameHelmetDverger.GetStableHashCode();
            demisterEffectHash = "Demister".GetStableHashCode();

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

            //////////
            ///            
            MaxSteps = config("General", "Max Steps", 3, "Define how many steps of focus the Dverger light beam has. Must be at least 2.");
            MinAngle = config("General", "Min Angle", 30.0f, "The angle of the beam at the narrowest setting.");
            MaxAngle = config("General", "Max Angle", 110.0f, "The angle of the beam at the widest setting.");
            MinIntensity = config("General", "Min Intensity", 1.4f, "The intensity of the beam at the widest setting.");
            MaxIntensity = config("General", "Max Intensity", 2.2f, "The intensity of the beam at the narrowest setting");
            MinRange = config("General", "Min Range", 45.0f, "The range of the beam at the narrowest setting.");
            MaxRange = config("General", "Max Range", 15.0f, "The range of the beam at the widest setting");
            PointIntensity = config("General", "Point Intensity", 1.1f, "The intensity of the Dverger light pool on the point light setting.");
            PointRange = config("General", "Point Range", 10.0f, "The range of the Dverger light pool on the point light setting.");
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

        public static void PatchCircletItemData(ItemDrop.ItemData item)
        {
            item.m_shared.m_maxQuality = 4;

            item.m_shared.m_durabilityPerLevel = durabilityPerLevel.Value;
        }

        public static void LogReqs(string eventname, Piece.Requirement[] requirements, int qualityLevel)
        {
            foreach (Piece.Requirement requirement in requirements)
            {
                if ((bool)requirement.m_resItem)
                {
                    LogInfo($"{eventname} {qualityLevel} {requirement.m_resItem.m_itemData.m_shared.m_name} {requirement.GetAmount(qualityLevel)}");
                }
            }

        }
    }
}