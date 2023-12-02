using System;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using BepInEx.Configuration;
using static CircletExtended.CircletExtended;
using System.Collections.Generic;

namespace CircletExtended
{
    [RequireComponent(typeof(Light))]
    public class DvergerLightController : MonoBehaviour
    {
        private ZNetView m_nview;
        
        private Light m_frontLight;
        private Light m_spotLight;

        private Player m_playerAttached;
        private ItemDrop.ItemData m_item;

        private int _maxLevel = 3;
        private float _minAngle = 30f;
        private float _maxAngle = 110f;
        private float _minIntensity = 1.4f;
        private float _maxIntensity = 2.2f;
        private float _minRange = 45f;
        private float _maxRange = 15f;
        private float _pointIntensity = 1.1f;
        private float _pointRange = 10f;
        private int _overloadCharges = 20;

        private bool forceOff = false;

        public int quality = 1;

        public LightState m_state = new LightState();

        const int intensityIncrement = 10;
        const int intensityFactorMax = 150;
        const int intensityFactorMin = 50;

        const float overloadIntensityInterval = 2f;
        const float overloadIntensityMin = 0.5f;
        const float overloadIntensityMax = 4f;

        private static int s_rayMaskSolids = 0;
        private static int s_rayMaskCharacters = 0;

        public class LightState
        {
            public bool on = true;
            public int level = 2;
            public int intensity = 100;
            public Color color = new Color(0.25f, 0.38f, 0.37f);
            public bool shadows = false;
            public bool spot = true;
            public float overload = 1f;
            public bool demister = false;
        }

        private void Awake()
        {
            m_nview = GetComponentInParent<ZNetView>();
        }

        private void Update()
        {
            if (!modEnabled.Value)
                return;

            GetSpotLight();

            var player = Player.m_localPlayer;
            if (player != null && player == m_playerAttached && player.TakeInput())
            {
                forceOff = disableOnSleep.Value && player.InBed();

                if (!forceOff && StateChanged(player))
                    SaveState();
            }

            UpdateLights();
        }

        private bool StateChanged(Player player)
        {
            foreach (int hotkey in hotkeys)
            {
                if (IsShortcutDown(hotkey, toggleShortcut))
                {
                    m_state.on = !m_state.on;
                    LogInfo($"Toggle {(m_state.on ? "on" : "off")}");
                    return true;
                }
                else if (IsShortcutDown(hotkey, toggleSpotShortcut))
                {
                    m_state.spot = !m_state.spot;
                    LogInfo($"Toggle spot {(m_state.spot ? "on" : "off")}");
                    return true;
                }

                if (!m_state.on)
                    continue;

                if (IsShortcutDown(hotkey, widenShortcut) && m_state.level > 0)
                {
                    m_state.level--;
                    LogInfo($"Widen {m_state.level}");
                    return true;
                }
                else if (IsShortcutDown(hotkey, narrowShortcut) && m_state.level < _maxLevel)
                {
                    m_state.level++;
                    
                    LogInfo($"Narrow {m_state.level}");
                    return true;
                }
                else if (IsShortcutDown(hotkey, increaseIntensityShortcut) && m_state.intensity < intensityFactorMax)
                {
                    m_state.intensity = Mathf.Clamp(m_state.intensity + intensityIncrement, intensityFactorMin, intensityFactorMax);
                    LogInfo($"Increase intensity {m_state.intensity}%");
                    return true;
                }
                else if (IsShortcutDown(hotkey, decreaseIntensityShortcut) && m_state.intensity > intensityFactorMin)
                {
                    m_state.intensity = Mathf.Clamp(m_state.intensity - intensityIncrement, intensityFactorMin, intensityFactorMax);
                    LogInfo($"Decrease intensity {m_state.intensity}%");
                    return true;
                }
                else if (IsShortcutDown(hotkey, toggleShadowsShortcut))
                {
                    m_state.shadows = !m_state.shadows;
                    LogInfo($"Toggle shadows {(m_state.spot ? "on" : "off")}");
                    return true;
                }
                else if (enableOverload.Value && IsShortcutDown(hotkey, overloadShortcut) && (!getFeaturesByUpgrade.Value || quality >= 3))
                {
                    LogInfo("Overload");
                    if (m_state.overload != 1f)
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$item_helmet_dverger: $hud_powernotready");
                    else
                        ApplyOverloadEffect(player);
                }
                else if (enableDemister.Value && IsShortcutDown(hotkey, toggleDemisterShortcut) && (!getFeaturesByUpgrade.Value || quality >= 4))
                {
                    m_state.demister = !m_state.demister;
                    LogInfo($"Toggle demister {(m_state.demister ? "on" : "off")}");
                }
            }

            return false;
        }

        private void ApplyOverloadEffect(Player player)
        {
            if (m_item == null)
                return;
            
            if (overloadEffect == null)
            {
                Incinerator incinerator = Resources.FindObjectsOfTypeAll<Incinerator>().FirstOrDefault();

                overloadEffect = Instantiate(incinerator.m_lightingAOEs);
                overloadEffect.name = "circletExtendedOverload";
                for (int i = overloadEffect.transform.childCount - 1; i > 0; i--)
                {
                    Transform child = overloadEffect.transform.GetChild(i);
                    switch (child.name)
                    {
                        case "AOE_ROD":
                        case "AOE_AREA":
                        case "Lighting":
                        case "lightning":
                        case "Lighting_rod":
                        case "Sparcs":
                        case "sfx_shockwave (1)":
                        case "poff_ring":
                        case "shockwave":
                        case "vfx_RockHit (1)":
                            child.parent = null;
                            Destroy(child.gameObject);
                            continue;
                        case "glow":
                            child.gameObject.SetActive(true);
                            continue;
                    }
                }
            }

            if (s_rayMaskSolids == 0)
            {
                s_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");
                s_rayMaskCharacters = LayerMask.GetMask("character", "character_net", "character_ghost", "character_noenv");
            }

            Instantiate(overloadEffect, player.transform.position, m_frontLight.transform.rotation);

            if (player.InWater())
            {
                player.AddLightningDamage(5f);
                player.AddStaggerDamage(player.GetStaggerTreshold() + 1f, -player.transform.forward);
            }

            StartCoroutine(OverloadIntensity());

            float radius = (float)Math.Tan((m_frontLight.spotAngle / 2) * (Math.PI / 180)) * m_frontLight.range;

            RaycastHit[] array = Physics.SphereCastAll(m_frontLight.transform.position, radius, m_frontLight.transform.forward, m_frontLight.range, s_rayMaskCharacters);
            if (array.Length != 0)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    RaycastHit hit = array[i];

                    GameObject gameObject = ((UnityEngine.Object)(object)hit.collider) ? Projectile.FindHitObject(hit.collider) : null;
                    bool hitCharacter = false;
                    IDestructible destructible = gameObject ? gameObject.GetComponent<IDestructible>() : null;
                    if (destructible != null)
                    {
                        hitCharacter = (destructible is Character);
                        if (!IsValidTarget(destructible))
                            continue;
                    }

                    if (!hitCharacter)
                        continue;

                    Character character = destructible as Character;

                    Vector3 charPos = character.m_head != null ? character.GetHeadPoint() : character.GetTopPoint();

                    Vector3 vector = charPos - m_frontLight.transform.position;
                    bool angleCheck = Vector3.Angle(m_frontLight.transform.forward, vector.normalized) <= (m_frontLight.spotAngle + 5) / 2;
                    float distance = Utils.DistanceXZ(m_frontLight.transform.position, charPos);

                    if (distance <= 5f || angleCheck && distance <= m_frontLight.range && !Physics.Linecast(m_frontLight.transform.position, charPos, s_rayMaskSolids))
                        character.AddStaggerDamage(character.GetStaggerTreshold() + 1f, vector.normalized);
                }
            }

            m_item.m_shared.m_useDurability = true;
            float cost = m_item.GetMaxDurability() / _overloadCharges;
            m_item.m_durability = Mathf.Max(0f, m_item.m_durability - cost);
        }

        public IEnumerator OverloadIntensity()
        {
            m_state.overload = overloadIntensityMax;

            float increment = ((overloadIntensityMax - overloadIntensityMin) / overloadIntensityInterval) * Time.fixedDeltaTime;

            while (m_state.overload > overloadIntensityMin)
            {
                m_state.overload -= increment; 
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(1f);

            while (m_state.overload <= 1.05f)
            {
                m_state.overload += increment;
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(1f);

            m_state.overload = 1f;
        }

        public bool IsValidTarget(IDestructible destr)
        {
            Character character = destr as Character;
            if ((bool)character)
            {
                if (character == m_playerAttached)
                    return false;

                if (m_playerAttached != null)
                {
                    bool flag = BaseAI.IsEnemy(m_playerAttached, character) || ((bool)character.GetBaseAI() && character.GetBaseAI().IsAggravatable() && m_playerAttached.IsPlayer());
                    if (!m_playerAttached.IsPlayer() && !flag)
                        return false;

                    if (m_playerAttached.IsPlayer() && !m_playerAttached.IsPVPEnabled() && !flag)
                        return false;
                }

                if (character.IsDodgeInvincible())
                    return false;
            }

            return true;
        }

        private bool IsShortcutDown(int hotkey, ConfigEntry<KeyboardShortcut> shortcut)
        {
            return hotkey == shortcut.Definition.GetHashCode() && (shortcut.Value.IsDown() || Input.GetKeyDown(shortcut.Value.MainKey) && !shortcut.Value.Modifiers.Any());
        }

        private void ShowMessage()
        {
            if (MessageHud.instance != null && m_playerAttached != null && Player.m_localPlayer == m_playerAttached)
            {
                MessageHud.instance.m_msgQeue.Clear();
                MessageHud.instance.m_msgQueueTimer = 1f;
                if (!forceOff && m_state.on)
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, $"$item_helmet_dverger: $msg_level {m_state.level} ({m_state.intensity}%)");
                else
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "$item_helmet_dverger: $hud_off");
            }
        }

        public void Initialize(Light light, Player player, ItemDrop.ItemData item)
        {
            m_frontLight = light;
            m_playerAttached = player;
            m_item = item;

            LoadState();

            UpdateLights();
        }

        private void LoadState()
        {
            if (StateLoaded())
                SetQuality();
        }

        private bool StateLoaded()
        {
            string stateJSON = "";

            if (m_item != null)
            {
                stateJSON = m_item.m_customData.GetValueSafe(customDataKey);
                quality = m_item.m_quality;
                LogInfo($"Loading state from m_customData: {stateJSON}");
            }

            if (String.IsNullOrWhiteSpace(stateJSON) && m_nview != null && m_nview.IsValid())
            {
                ZDO zdo = m_nview.GetZDO();

                int @int = zdo.GetInt(ZDOVars.s_dataCount);
                for (int i = 0; i < @int; i++)
                    if (zdo.GetString($"data_{i}") == customDataKey)
                        stateJSON = zdo.GetString($"data__{i}");

                quality = zdo.GetInt(ZDOVars.s_quality, 1);
                LogInfo($"Loading state from m_nview: {stateJSON}");
            }

            if (!String.IsNullOrWhiteSpace(stateJSON))
            {
                try
                {
                    m_state = JsonUtility.FromJson<LightState>(stateJSON);
                    return true;
                }
                catch (Exception e)
                {
                    LogInfo($"State parsing error:\n{e}");
                }
            }

            return false;
        }

        private void SaveState()
        {
            if (m_nview == null || !m_nview.IsValid())
                return;

            if (m_item == null)
                return;

            m_state.color = circletColor.Value;

            m_item.m_customData[customDataKey] = JsonUtility.ToJson(m_state);

            ShowMessage();
        }

        private void SetQuality()
        {
            _maxLevel = Mathf.Clamp(MaxSteps.Value, 1, 50);
            _minAngle = Mathf.Clamp(MinAngle.Value, 1, 360);
            _maxAngle = Mathf.Clamp(MaxAngle.Value, 1, 360);
            _minIntensity = Mathf.Clamp(MaxIntensity.Value, 0, 10);
            _maxIntensity = Mathf.Clamp(MinIntensity.Value, 0, 10);
            _minRange = Mathf.Clamp(MinRange.Value, 0, 1000);
            _maxRange = Mathf.Clamp(MaxRange.Value, 0, 1000);
            _pointIntensity = Mathf.Clamp(PointIntensity.Value, 0, 10);
            _pointRange = Mathf.Clamp(PointRange.Value, 0, 1000);

            _overloadCharges = getFeaturesByUpgrade.Value ? 25 * quality : 100;
        }

        private void UpdateLights()
        {
            if (m_state.level == _maxLevel)
            {
                m_frontLight.type = LightType.Point;
                m_frontLight.intensity = _pointIntensity;
                m_frontLight.range = _pointRange;
            }
            else
            {
                float t = (float) m_state.level / Math.Max(_maxLevel - 1, 1);

                m_frontLight.type = LightType.Spot;
                m_frontLight.spotAngle = Mathf.Lerp(_minAngle, _maxAngle, t);
                m_frontLight.intensity = Mathf.Lerp(_minIntensity, _maxIntensity, t);
                m_frontLight.range = Mathf.Lerp(_minRange, _maxRange, t);
            }

            float intensityFactor = m_state.intensity / 100f;

            m_frontLight.range *= intensityFactor;
            m_frontLight.intensity *= intensityFactor;
            m_frontLight.intensity *= m_state.overload;
            
            float qualityFactor = 1f + quality * 0.05f;
            m_frontLight.range *= qualityFactor;
            m_frontLight.intensity *= qualityFactor;

            m_frontLight.enabled = !forceOff && m_state.on;
            m_frontLight.shadows = enableShadows.Value && m_state.shadows && m_state.level != _maxLevel ? LightShadows.Soft : LightShadows.None;

            if (m_spotLight != null)
            {
                m_spotLight.enabled = !forceOff && m_state.spot;
                m_spotLight.intensity = m_state.overload;
            }

            if (m_playerAttached != null && m_nview != null && m_nview.IsValid())
            {
                SEMan seman = m_playerAttached.GetSEMan();
                if (m_state.demister && seman.GetStatusEffect(demisterEffectHash) == null)
                    seman.AddStatusEffect(demisterEffectHash);
                else if (!m_state.demister && seman.GetStatusEffect(demisterEffectHash) != null)
                    seman.RemoveStatusEffect(demisterEffectHash);
            }
                
        }
        
        private void GetSpotLight()
        {
            if (!m_playerAttached)
                return;

            if (m_spotLight != null)
                return;

            foreach (Light light in m_frontLight.GetComponentsInParent<Light>())
                if (light != m_frontLight)
                    m_spotLight = light;

            if (m_spotLight != null)
                UpdateLights();
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachItem))]
    public static class VisEquipment_AttachItem_Patch
    {
        private static void Postfix(VisEquipment __instance, GameObject __result, int itemHash)
        {
            if (!modEnabled.Value)
                return;

            if (itemHash != itemHashHelmetDverger || !__instance.m_isPlayer || __result == null)
                return;

            DvergerLightController component = __result.GetComponent<DvergerLightController>();
            if (component != null)
                Object.Destroy(component);

            Light[] lights = __result.GetComponentsInChildren<Light>();
            if (lights.Length == 0)
                return;

            Player player = __instance.GetComponentInParent<Player>();
            if (player == null)
                return;

            ItemDrop.ItemData item = player.GetCirclet().circlet == null ? player.m_helmetItem : player.GetCirclet().circlet;
            if (item == null)
                return;

            if (item.m_dropPrefab.name != itemNameHelmetDverger)
                return;

            instance.ConfigInit();

            __result.AddComponent<DvergerLightController>().Initialize(lights[0], player, item);

            item.m_shared.m_useDurability = item.GetDurabilityPercentage() != 1f;
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
    public static class ItemDrop_Start_Patch
    {
        private static void Postfix(ItemDrop __instance)
        {
            if (!modEnabled.Value)
                return;

            if (__instance.GetPrefabName(__instance.name) != itemNameHelmetDverger)
                return;

            DvergerLightController component = __instance.GetComponentInChildren<DvergerLightController>();
            if (component != null)
                Object.Destroy(component);

            Light[] lights = __instance.GetComponentsInChildren<Light>();
            if (lights.Length == 0)
                return;

            lights[0].gameObject.transform.parent.gameObject.AddComponent<DvergerLightController>().Initialize(lights[0], null, __instance.m_itemData);
        }
    }

    [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.SetVisualItem))]
    public static class ItemStand_SetVisualItem_Patch
    {
        private static void Prefix(ItemStand __instance, GameObject ___m_visualItem, string ___m_visualName, int ___m_visualVariant, string itemName, int variant, ref bool __state)
        {
            if (!modEnabled.Value)
                return;

            if (__instance.GetAttachedItem() != itemNameHelmetDverger)
                return;

            if (___m_visualItem != null)
                return;

            if (___m_visualName == itemName && ___m_visualVariant == variant)
                return;

            __state = true;
        }

        private static void Postfix(GameObject ___m_visualItem, bool __state)
        {
            if (!modEnabled.Value)
                return;

            if (!__state)
                return;

            DvergerLightController component = ___m_visualItem.GetComponentInChildren<DvergerLightController>();
            if (component != null)
                Object.Destroy(component);

            Light[] lights = ___m_visualItem.GetComponentsInChildren<Light>();
            if (lights.Length == 0)
                return;

            ___m_visualItem.AddComponent<DvergerLightController>().Initialize(lights[0], null, null);
        }

    }

    [HarmonyPatch(typeof(ArmorStand), nameof(ArmorStand.SetVisualItem))]
    public static class ArmorStand_SetVisualItem_Patch
    {
        private static void Prefix(ArmorStand __instance, int index, string itemName, int variant, List<ArmorStand.ArmorStandSlot> ___m_slots, ref bool __state)
        {
            if (!modEnabled.Value)
                return;

            ArmorStand.ArmorStandSlot armorStandSlot = ___m_slots[index];
            if (armorStandSlot.m_visualName == itemName && armorStandSlot.m_visualVariant == variant)
            {
                return;
            }

            /*if (__instance.GetAttachedItem() != itemNameHelmetDverger)
                return;

            if (___m_visualItem != null)
                return;

            if (___m_visualName == itemName && ___m_visualVariant == variant)
                return;

            __state = true;*/
        }

        private static void Postfix(ArmorStand __instance)
        {
            if (!modEnabled.Value)
                return;

            /*if (!__state)
                return;

            DvergerLightController component = ___m_visualItem.GetComponentInChildren<DvergerLightController>();
            if (component != null)
                Object.Destroy(component);

            Light[] lights = ___m_visualItem.GetComponentsInChildren<Light>();
            if (lights.Length == 0)
                return;

            ___m_visualItem.AddComponent<DvergerLightController>().Initialize(lights[0], null, null);*/
        }

    }
    




}
