using System;
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

        public static ConfigEntry<int> maxQuality;
        public static ConfigEntry<bool> enableOverload;

        public static ConfigEntry<Color> circletColor;

        public static ConfigEntry<bool> disableOnSleep;
        public static ConfigEntry<bool> enableShadows;

        public static ConfigEntry<KeyboardShortcut> widenShortcut;
        public static ConfigEntry<KeyboardShortcut> narrowShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleShortcut;
        public static ConfigEntry<KeyboardShortcut> overloadShortcut;

        public static ConfigEntry<KeyboardShortcut> increaseIntensityShortcut;
        public static ConfigEntry<KeyboardShortcut> decreaseIntensityShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleShadowsShortcut;
        public static ConfigEntry<KeyboardShortcut> toggleSpotShortcut;

        public static bool noModifiersSet = true;

        internal static CircletExtended instance;

        public const string itemNameHelmetDverger = "HelmetDverger";
        public static int itemHashHelmetDverger = 703889544;
        public static GameObject overloadEffect;

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

        private void ConfigInit()
        {
            config("General", "NexusID", 0, "Nexus mod ID for updates", false);

            modEnabled = config("General", "Enabled", defaultValue: true, "Enable the mod.");
            configLocked = config("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only.");
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging. [Not Synced with Server]", false);

            maxQuality = config("Circlet", "Max quality", defaultValue: 4, "Max circlet quality.");
            enableOverload = config("Circlet", "Enable overload", defaultValue: true, "Enables overload. Press hotkey to blind opponents with a bright flash at the cost of some circlet durability");

            circletColor = config("Circlet", "Light color", defaultValue: Color.white, "Circlet beam light color. Changing this ingame will change current circlet color [Not Synced with Server]", false);
            disableOnSleep = config("Circlet", "Disable when sleeping", defaultValue: true, "Turn off the light when sleeping. [Not Synced with Server]", false);
            enableShadows = config("Circlet", "Enables optional shadows", defaultValue: false, "Enables option to toggle shadows. Will have impact on the performance. [Not Synced with Server]", false);

            widenShortcut = config("Hotkeys", "Beam widen", defaultValue: new KeyboardShortcut(KeyCode.RightArrow), "Widen beam shortcut. [Not Synced with Server]", false);
            narrowShortcut = config("Hotkeys", "Beam narrow", defaultValue: new KeyboardShortcut(KeyCode.LeftArrow), "Narrow beam shortcut. [Not Synced with Server]", false);
            toggleShortcut = config("Hotkeys", "Toggle light", defaultValue: new KeyboardShortcut(KeyCode.UpArrow), "Toggle main light shortcut. [Not Synced with Server]", false);
            toggleSpotShortcut = config("Hotkeys", "Toggle radiance", defaultValue: new KeyboardShortcut(KeyCode.DownArrow), "Toggle spotlight shortcut. [Not Synced with Server]", false);

            increaseIntensityShortcut = config("Hotkeys", "Intensity increase", defaultValue: new KeyboardShortcut(KeyCode.Minus), "Increase intensity shortcut. [Not Synced with Server]", false);
            decreaseIntensityShortcut = config("Hotkeys", "Intensity decrease", defaultValue: new KeyboardShortcut(KeyCode.Equals), "Decrease intensity shortcut. [Not Synced with Server]", false);
            toggleShadowsShortcut = config("Hotkeys", "Toggle shadows", defaultValue: new KeyboardShortcut(KeyCode.Home), "Toggle shadows shortcut. [Not Synced with Server]", false);
            overloadShortcut = config("Hotkeys", "Overload", defaultValue: new KeyboardShortcut(KeyCode.T), "Overload shortcut. [Not Synced with Server]", false);
            
            itemHashHelmetDverger = itemNameHelmetDverger.GetStableHashCode();

            noModifiersSet = noModifiersSet && !widenShortcut.Value.Modifiers.Any();
            noModifiersSet = noModifiersSet && !narrowShortcut.Value.Modifiers.Any();
            noModifiersSet = noModifiersSet && !toggleShortcut.Value.Modifiers.Any();
            noModifiersSet = noModifiersSet && !overloadShortcut.Value.Modifiers.Any();
            noModifiersSet = noModifiersSet && !increaseIntensityShortcut.Value.Modifiers.Any();
            noModifiersSet = noModifiersSet && !decreaseIntensityShortcut.Value.Modifiers.Any();
            noModifiersSet = noModifiersSet && !toggleShadowsShortcut.Value.Modifiers.Any();
            noModifiersSet = noModifiersSet && !toggleSpotShortcut.Value.Modifiers.Any();

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

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, defaultValue, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, string description, bool synchronizedSetting = true) => config(group, name, defaultValue, new ConfigDescription(description), synchronizedSetting);

    }
}