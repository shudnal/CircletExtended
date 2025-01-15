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
    public class DvergerLightController : MonoBehaviour
    {
        private ZNetView m_nview;
        
        private Light m_frontLight;
        private Light m_spotLight;

        private Player m_playerAttached;
        private ItemDrop.ItemData m_item;
        private int m_zdoIndex;
        private GameObject m_visual;

        private LightState m_state = new LightState();

        private ParticleSystemForceField m_overloadDemister;

        private int m_maxLevel = 3;
        private float m_minAngle = 30f;
        private float m_maxAngle = 110f;
        private float m_minIntensity = 1.4f;
        private float m_maxIntensity = 2.2f;
        private float m_minRange = 45f;
        private float m_maxRange = 15f;
        private float m_pointIntensity = 1.1f;
        private float m_pointRange = 10f;
        private float m_spotIntensity = 0.8f;
        private float m_spotRange = 8f;
        private int m_overloadCharges = 20;

        private MeshRenderer m_gemRenderer;
        private Color m_gemColor;

        const int intensityIncrement = 10;
        const int intensityFactorMax = 150;
        const int intensityFactorMin = 50;

        const float overloadIntensityInterval = 2f;
        const float overloadIntensityMin = 0.5f;
        const float overloadIntensityMax = 4f;

        private static int s_rayMaskSolids = 0;
        private static int s_rayMaskCharacters = 0;

        private static readonly MaterialPropertyBlock s_matBlock = new MaterialPropertyBlock();

        public static GameObject overloadEffect;
        public const string overloadItemName = "circletExtendedOverload";
        public static int overloadItemHash = overloadItemName.GetStableHashCode();
        public static Color overloadColor;

        public static int demisterEffectHash = "Demister".GetStableHashCode();

        public static GameObject demisterForceField;
        public const string forceFieldDemisterName = "Particle System Force Field";

        public static int s_lightMaskNonPlayer;

        private float m_updateVisualTimer = 0f;

        private static readonly List<DvergerLightController> Instances = new List<DvergerLightController>();
        private static readonly Dictionary<ItemDrop.ItemData, LightState> itemState = new Dictionary<ItemDrop.ItemData, LightState>();

        const int c_characterLayer = 9;

        public static readonly int s_stateHash = "circletStateHash".GetStableHashCode();
        public static readonly int s_state = "circletState".GetStableHashCode();

        [Serializable]
        private class LightState
        {
            public bool on = true;
            public int level = 2;
            public int intensity = 100;
            public Color color = new Color(0.25f, 0.38f, 0.37f);
            public bool shadows = true;
            public bool spot = false;
            public bool overloaded = false;
            public float overload = 1f;
            public bool demister = false;
            public int quality = 1;
            public bool demisted = false;
            public float demistedrange = 1f;
            public bool forceoff = false;

            [NonSerialized]
            public int hash = 0;

            [NonSerialized]
            public string json = "";

            public void UpdateHash()
            {
                json = JsonUtility.ToJson(this);
                hash = json.GetStableHashCode();
            }
        }

        private void Awake()
        {
            m_nview = GetComponentInParent<ZNetView>();
            m_gemRenderer = GetComponentInChildren<MeshRenderer>();
            
            if (s_lightMaskNonPlayer == 0)
                s_lightMaskNonPlayer = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle", "item");
        }

        private void Start()
        {
            UpdateSpotLight();

            m_visual = m_playerAttached?.GetVisual();
            UpdateVisualLayers();
        }

        void FixedUpdate()
        {
            if (m_updateVisualTimer > 0)
            {
                m_updateVisualTimer = Mathf.Max(0f, m_updateVisualTimer - Time.fixedDeltaTime);

                if (m_updateVisualTimer == 0f)
                    UpdateVisualLayers();
            }
        }

        void OnEnable()
        {
            Instances.Add(this);
            
            if (m_item != null)
                itemState[m_item] = m_state;
        }

        void OnDisable()
        {
            Instances.Remove(this);

            if (m_item != null)
                itemState.Remove(m_item);
        }

        private void ApplyGemColor(Color gemColor)
        {
            if (m_gemRenderer == null)
                return;

            if (m_gemColor == gemColor)
                return;

            m_gemColor = gemColor;

            m_gemRenderer.GetPropertyBlock(s_matBlock, 0);
            s_matBlock.SetColor("_EmissionColor", m_gemColor);
            m_gemRenderer.SetPropertyBlock(s_matBlock, 0);
        }

        private void Update()
        {
            Player player = Player.m_localPlayer;
            if (player != null && player == m_playerAttached && player.TakeInput() && StateChanged(player))
                SaveState(changedByPlayer:true);

            UpdateLights();
        }

        private void OnDestroy()
        {
            Player player = m_playerAttached;
            if (player != null && m_state.demister && player.GetSEMan().GetStatusEffect(demisterEffectHash) != null 
                    && (player.m_utilityItem == null || player.m_utilityItem.m_shared.m_name != "$item_demister"))
                player.GetSEMan().RemoveStatusEffect(demisterEffectHash);
        }

        private bool QualityLevelAvailable(int quality)
        {
            return !getFeaturesByUpgrade.Value || m_state.quality >= quality;
        }

        private bool ForceOff()
        {
            return disableOnSleep.Value && m_playerAttached != null && m_playerAttached.InBed();
        }

        private bool StateChanged(Player player)
        {
            if (m_item != null)
                m_state.quality = m_item.m_quality;

            if (m_state.forceoff != ForceOff())
            {
                m_state.forceoff = ForceOff();
                LogInfo($"Force off {(m_state.forceoff ? "enabled" : "disabled")}");
                return true;
            }
            
            foreach (int hotkey in hotkeys)
            {
                if (IsShortcutDown(hotkey, toggleShortcut))
                {
                    m_state.on = !m_state.on;
                    LogInfo($"Toggle {(m_state.on ? "on" : "off")}");
                    return true;
                }
                else if (QualityLevelAvailable(2) && m_spotLight && IsShortcutDown(hotkey, toggleSpotShortcut))
                {
                    m_state.spot = !m_state.spot;
                    LogInfo($"Toggle spot {(m_state.spot ? "on" : "off")}");
                    return true;
                }
                else if (enableDemister.Value && QualityLevelAvailable(4) && IsShortcutDown(hotkey, toggleDemisterShortcut))
                {
                    m_state.demister = !m_state.demister;
                    LogInfo($"Toggle demister {(m_state.demister ? "on" : "off")}");
                    return true;
                }

                if (!m_state.on)
                    continue;

                if (m_state.level > 0 && IsShortcutDown(hotkey, widenShortcut))
                {
                    m_state.level--;
                    LogInfo($"Widen {m_state.level}");
                    return true;
                }
                else if (m_state.level < m_maxLevel && IsShortcutDown(hotkey, narrowShortcut))
                {
                    m_state.level++;
                    
                    LogInfo($"Narrow {m_state.level}");
                    return true;
                }
                else if (m_state.intensity < intensityFactorMax && IsShortcutDown(hotkey, increaseIntensityShortcut))
                {
                    m_state.intensity = Mathf.Clamp(m_state.intensity + intensityIncrement, intensityFactorMin, intensityFactorMax);
                    LogInfo($"Increase intensity {m_state.intensity}%");
                    return true;
                }
                else if (m_state.intensity > intensityFactorMin && IsShortcutDown(hotkey, decreaseIntensityShortcut))
                {
                    m_state.intensity = Mathf.Clamp(m_state.intensity - intensityIncrement, intensityFactorMin, intensityFactorMax);
                    LogInfo($"Decrease intensity {m_state.intensity}%");
                    return true;
                }
                else if (IsShortcutDown(hotkey, toggleShadowsShortcut))
                {
                    m_state.shadows = !m_state.shadows;
                    LogInfo($"Toggle shadows {(m_state.shadows ? "on" : "off")}");
                    return true;
                }
                else if (enableOverload.Value && QualityLevelAvailable(3) && IsShortcutDown(hotkey, overloadShortcut))
                {
                    LogInfo("Overload");
                    if (m_state.overloaded)
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$item_helmet_dverger: $hud_powernotready");
                    else
                        ApplyOverloadEffect(player);
                    
                    return true;
                }
            }

            return false;
        }

        private void ApplyOverloadEffect(Player player)
        {
            if (m_item == null)
                return;
            
            if (s_rayMaskSolids == 0)
            {
                s_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");
                s_rayMaskCharacters = LayerMask.GetMask("character", "character_net", "character_ghost", "character_noenv");
            }

            ZNetScene.instance.SpawnObject(player.transform.position, m_frontLight.transform.rotation, overloadEffect);

            if (player.InWater())
            {
                player.AddLightningDamage(5f);
                player.AddStaggerDamage(player.GetStaggerTreshold() + 1f, -player.transform.forward);
            }

            StartCoroutine(OverloadIntensity());

            if (enableOverloadDemister.Value && m_overloadDemister != null && overloadDemisterTime.Value != 0f)
                StartCoroutine(OverloadDemister());

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
            m_item.m_durability = Mathf.Max(m_item.m_durability - (m_item.GetMaxDurability() / m_overloadCharges), 0f);
        }

        public IEnumerator OverloadIntensity()
        {
            m_state.overloaded = true;
            m_state.overload = overloadIntensityMax;

            float increment = ((overloadIntensityMax - overloadIntensityMin) / overloadIntensityInterval) * Time.fixedDeltaTime;

            while (m_state.overload > overloadIntensityMin)
            {
                m_state.overload -= increment;
                SaveState();
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(1f);

            while (m_state.overload <= 1.05f)
            {
                m_state.overload += increment;
                SaveState();
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(1f);

            while (m_state.overload >= 1f)
            {
                m_state.overload -= increment;
                SaveState();
                yield return new WaitForFixedUpdate();
            }

            m_state.overload = 1f;
            m_state.overloaded = false;
            SaveState();
        }

        public IEnumerator OverloadDemister()
        {
            m_state.demisted = true;
            SaveState();

            for (int i = 0; i < overloadDemisterTime.Value; i++)
            {
                m_state.demistedrange = 10f + (overloadDemisterRange.Value - 10f) * (1f - i / overloadDemisterTime.Value);
                yield return new WaitForSeconds(1f);
            }

            m_state.demisted = false;
            SaveState();
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
                if (!m_state.forceoff && m_state.on)
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, $"$item_helmet_dverger: $msg_level {m_state.level} ({m_state.intensity}%)");
                else
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "$item_helmet_dverger: $hud_off");
            }
        }

        public void Initialize(Light light, Player player = null, ItemDrop.ItemData item = null, int zdoIndex = 0)
        {
            m_frontLight = light;
            m_playerAttached = player;
            m_item = item;
            m_zdoIndex = zdoIndex;

            if (enableOverloadDemister.Value && player && (bool)demisterForceField && !m_overloadDemister)
            {
                GameObject demister = Instantiate(demisterForceField, transform);
                demister.name = forceFieldDemisterName;
                demister.SetActive(false);
                m_overloadDemister = demister.GetComponent<ParticleSystemForceField>();
                m_overloadDemister.endRange = overloadDemisterRange.Value;
            }

            LoadState();
        }

        private void LoadState()
        {
            UpdateQualityLevel();

            if (m_state.overloaded || m_state.demisted || m_nview != null && m_nview.IsValid() && m_nview.IsOwner() && m_nview.GetZDO().GetInt(s_stateHash, 0) == 0)
            {
                m_state.overloaded = false;
                m_state.demisted = false;
                SaveState();
            }

            if (m_item != null)
                itemState[m_item] = m_state;
        }

        private void UpdateQualityLevel()
        {
            if (IsStateLoaded())
                SetQuality();
        }

        private bool IsStateLoaded()
        {
            string stateJSON = "";

            if (m_item != null)
            {
                stateJSON = m_item.m_customData.GetValueSafe(customDataKey);
                if (string.IsNullOrWhiteSpace(stateJSON))
                {
                    m_state = new LightState
                    {
                        quality = m_item.m_quality
                    };
                    SaveState();
                }

                LogInfo($"Loading state from item: {stateJSON}");
            }

            if (string.IsNullOrWhiteSpace(stateJSON) && m_nview != null && m_nview.IsValid())
            {
                ZDO zdo = m_nview.GetZDO();

                ItemDrop.ItemData item = new ItemDrop.ItemData();
                ItemDrop.LoadFromZDO(item, zdo);

                stateJSON = item.m_customData.GetValueSafe(customDataKey);

                LogInfo($"Loading state from item zdo: {stateJSON}");

                if (string.IsNullOrWhiteSpace(stateJSON))
                {
                    ItemDrop.LoadFromZDO(m_zdoIndex, item, zdo);
                    stateJSON = item.m_customData.GetValueSafe(customDataKey);
                    
                    LogInfo($"Loading state from zdo index {m_zdoIndex}: {stateJSON}");
                }

                if (string.IsNullOrWhiteSpace(stateJSON))
                {
                    stateJSON = zdo.GetString(s_state);
                    LogInfo($"Loading state from zdo state: {stateJSON}");
                }
            }

            if (!string.IsNullOrWhiteSpace(stateJSON))
            {
                try
                {
                    m_state = JsonUtility.FromJson<LightState>(stateJSON);
                }
                catch (Exception e)
                {
                    LogInfo($"State parsing error:\n{e}");
                    return false;
                }
            }

            return true;
        }

        private void SaveState(bool changedByPlayer = false)
        {
            m_state.color = circletColor.Value;

            m_state.UpdateHash();

            if (m_item != null)
                m_item.m_customData[customDataKey] = m_state.json;

            if (m_nview != null && m_nview.IsValid() && m_nview.IsOwner())
            {
                m_nview.GetZDO().Set(s_stateHash, m_state.hash);
                m_nview.GetZDO().Set(s_state, m_state.json);
            }

            if (changedByPlayer)
            {
                if (m_item != null)
                    itemState[m_item] = m_state;

                ShowMessage();
            }

            m_state.hash = 0;
        }

        private void SetQuality()
        {
            m_maxLevel = Mathf.Clamp(GetMaxSteps(m_state.quality), 1, 50);
            m_minAngle = Mathf.Clamp(GetMinAngle(m_state.quality), 1, 360);
            m_maxAngle = Mathf.Clamp(GetMaxAngle(m_state.quality), 1, 360);
            m_minIntensity = Mathf.Clamp(GetMaxIntensity(m_state.quality), 0, 10);
            m_maxIntensity = Mathf.Clamp(GetMinIntensity(m_state.quality), 0, 10);
            m_minRange = Mathf.Clamp(GetMinRange(m_state.quality), 0, 1000);
            m_maxRange = Mathf.Clamp(GetMaxRange(m_state.quality), 0, 1000);
            m_pointIntensity = Mathf.Clamp(GetPointIntensity(m_state.quality), 0, 10);
            m_pointRange = Mathf.Clamp(GetPointRange(m_state.quality), 0, 1000);
            m_spotIntensity = Mathf.Clamp(GetSpotIntensity(m_state.quality), 0, 10);
            m_spotRange = Mathf.Clamp(GetSpotRange(m_state.quality), 0, 1000);

            m_overloadCharges = overloadChargesPerLevel.Value * (QualityLevelAvailable(4) ? 2 : 1);
        }

        private void UpdateLights()
        {
            bool stateChanged = m_nview != null && m_nview.IsValid() && m_nview.GetZDO().GetInt(s_stateHash, 0) != m_state.hash && IsStateLoaded();
            if (!stateChanged && m_state.hash != 0)
                return;

            m_state.UpdateHash();

            if (m_state.level == m_maxLevel)
            {
                m_frontLight.type = LightType.Point;
                m_frontLight.intensity = m_pointIntensity;
                m_frontLight.range = m_pointRange;
            }
            else
            {
                float t = (float) m_state.level / Math.Max(m_maxLevel - 1, 1);

                m_frontLight.type = LightType.Spot;
                m_frontLight.spotAngle = Mathf.Lerp(m_minAngle, m_maxAngle, t);
                m_frontLight.intensity = Mathf.Lerp(m_minIntensity, m_maxIntensity, t);
                m_frontLight.range = Mathf.Lerp(m_minRange, m_maxRange, t);
            }

            m_frontLight.color = !m_state.overloaded ? m_state.color : Color.Lerp(m_state.color, overloadColor, (m_state.overload - 1.05f) / (overloadIntensityMax - overloadIntensityMin));

            float intensityFactor = m_state.intensity / 100f;

            m_frontLight.range *= intensityFactor;
            m_frontLight.intensity *= intensityFactor;
            
            if (m_state.overloaded)
                m_frontLight.intensity *= m_state.overload;
            
            float qualityFactor = 1f + m_state.quality * 0.05f;
            m_frontLight.range *= qualityFactor;
            m_frontLight.intensity *= qualityFactor;

            m_frontLight.enabled = !m_state.forceoff && m_state.on;
            m_frontLight.shadows = enableShadows.Value && m_state.shadows && m_state.level != m_maxLevel ? LightShadows.Soft : LightShadows.None;
            m_frontLight.shadowStrength = 1f - m_state.level * 0.1f;

            if (m_spotLight)
            {
                m_spotLight.enabled = !m_state.forceoff && m_state.spot;
                m_spotLight.color = m_frontLight.color;
                m_spotLight.shadows = enableShadows.Value && m_state.shadows && m_state.level != m_maxLevel ? LightShadows.Soft : LightShadows.None;
                m_spotLight.shadowStrength = m_playerAttached != null && m_playerAttached.InInterior() ? 0.9f : 0.8f;
                m_spotLight.cullingMask = s_lightMaskNonPlayer;
                m_spotLight.intensity = m_spotIntensity;
                m_spotLight.range = m_spotRange;


                if (m_state.overloaded)
                    m_spotLight.intensity = m_state.overload;
            }

            if (m_playerAttached != null && m_nview != null && m_nview.IsValid() && QualityLevelAvailable(4) && (m_playerAttached.m_utilityItem == null || m_playerAttached.m_utilityItem.m_shared.m_name != "$item_demister"))
            {
                SEMan seman = m_playerAttached.GetSEMan();
                if (m_state.demister && seman.GetStatusEffect(demisterEffectHash) == null)
                    seman.AddStatusEffect(demisterEffectHash);
                else if (!m_state.demister && seman.GetStatusEffect(demisterEffectHash) != null)
                    seman.RemoveStatusEffect(demisterEffectHash);
            }

            if ((bool)m_overloadDemister)
            {
                m_overloadDemister.endRange = m_state.demistedrange;
                m_overloadDemister.gameObject.SetActive(m_state.demisted);
            }

            ApplyGemColor(m_state.color);
        }
        
        private void UpdateSpotLight()
        {
            if (enableSpotLight.Value && !m_spotLight && !gameObject.TryGetComponent(out m_spotLight))
                m_spotLight = gameObject.AddComponent<Light>();
            else if (!enableSpotLight.Value && m_spotLight)
            {
                Destroy(m_spotLight);
                m_spotLight = null;
            }
        }

        private void UpdateVisualLayers()
        {
            if (m_visual == null)
                return;

            for (int i = 0; i < m_visual.transform.childCount; i++)
            {
                Transform child = m_visual.transform.GetChild(i);
                if (child.gameObject.layer == c_characterLayer)
                    continue;

                child.gameObject.layer = c_characterLayer;

                Transform[] children = child.GetComponentsInChildren<Transform>(includeInactive: true);
                foreach (Transform chld in children)
                    chld.gameObject.layer = c_characterLayer;
            }
        }

        private void StartUpdateVisualLayers()
        {
            m_updateVisualTimer = 0.5f;
        }

        internal static void UpdateVisualsLayers(GameObject visual)
        {
            foreach (DvergerLightController instance in Instances)
                if (instance.m_visual == visual)
                    instance.StartUpdateVisualLayers();
        }

        public static bool IsCircletLightEnabled(ItemDrop.ItemData item) => item != null && itemState.ContainsKey(item) && itemState[item].on;

        public static void UpdateSpotLights() => Instances.Do(controller => controller.UpdateSpotLight());

        public static void UpdateQualityLevels() => Instances.Do(controller => controller.UpdateQualityLevel());

        public static void RegisterEffects()
        {
            demisterForceField = ObjectDB.instance.GetItemPrefab("Demister")?.transform.Find(forceFieldDemisterName)?.gameObject;

            if (!(bool)overloadEffect)
            {
                Incinerator incinerator = Resources.FindObjectsOfTypeAll<Incinerator>().FirstOrDefault();

                overloadEffect = CustomPrefabs.InitPrefabClone(incinerator.m_lightingAOEs, overloadItemName);
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
                            UnityEngine.Object.Destroy(child.gameObject);
                            continue;
                        case "glow":
                            child.gameObject.SetActive(true);
                            continue;
                        case "Point light (1)":
                            overloadColor = child.GetComponent<Light>().color;
                            continue;
                    }
                }
            }

            if ((bool)overloadEffect && ZNetScene.instance && !ZNetScene.instance.m_namedPrefabs.ContainsKey(overloadItemHash))
            {
                ZNetScene.instance.m_prefabs.Add(overloadEffect);
                ZNetScene.instance.m_namedPrefabs.Add(overloadItemHash, overloadEffect);
            }
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachItem))]
    public static class VisEquipment_AttachItem_HumanoidAttachment
    {
        private static void Postfix(VisEquipment __instance, GameObject __result, int itemHash)
        {
            if (itemHash != CircletItem.itemHashHelmetDverger || __result == null)
                return;

            DvergerLightController component = __result.GetComponent<DvergerLightController>();
            if (component != null)
                Object.Destroy(component);

            Light[] lights = __result.GetComponentsInChildren<Light>(includeInactive: true);
            if (lights.Length == 0)
                return;

            ItemDrop.ItemData item = null;
            Player player = null;
            if (__instance.m_isPlayer)
            {
                player = __instance.GetComponentInParent<Player>(includeInactive: true);
                if (player == null)
                    return;

                item = player.GetCirclet();
                if (item == null && CircletItem.IsCircletItemData(player.m_helmetItem) && __instance.m_currentHelmetItemHash == itemHash)
                    item = player.m_helmetItem;

                if (item != null)
                    CircletItem.PatchCircletItemData(item);
            }

            __instance.UpdateVisuals();

            __result.AddComponent<DvergerLightController>().Initialize(lights[0], player, item);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
    public static class ItemDrop_Start_ItemDropAttachment
    {
        private static void Postfix(ItemDrop __instance)
        {
            if (!visualStateItemDrop.Value)
                return;

            if (!CircletItem.IsCircletItem(__instance))
                return;

            DvergerLightController component = __instance.GetComponentInChildren<DvergerLightController>(includeInactive: true);
            if (component != null)
                Object.Destroy(component);

            Light[] lights = __instance.GetComponentsInChildren<Light>(includeInactive: true);
            if (lights.Length == 0)
                return;

            lights[0].gameObject.transform.parent.gameObject.AddComponent<DvergerLightController>().Initialize(lights[0], item:__instance.m_itemData);
        }
    }

    [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.SetVisualItem))]
    public static class ItemStand_SetVisualItem_ItemStandAttachment
    {
        private static void Prefix(ItemStand __instance, GameObject ___m_visualItem, string ___m_visualName, int ___m_visualVariant, string itemName, int variant, ref bool __state)
        {
            if (!visualStateItemStand.Value)
                return;

            if (!CircletItem.IsCircletItemName(__instance.GetAttachedItem()))
                return;

            if (___m_visualItem != null)
                return;

            if (___m_visualName == itemName && ___m_visualVariant == variant)
                return;

            __state = true;
        }

        private static void Postfix(GameObject ___m_visualItem, bool __state)
        {
            if (!__state)
                return;

            DvergerLightController component = ___m_visualItem.GetComponentInChildren<DvergerLightController>(includeInactive: true);
            if (component != null)
                Object.Destroy(component);

            Light[] lights = ___m_visualItem.GetComponentsInChildren<Light>(includeInactive: true);
            if (lights.Length == 0)
                return;

            ___m_visualItem.AddComponent<DvergerLightController>().Initialize(lights[0]);
        }

    }

    [HarmonyPatch(typeof(ArmorStand), nameof(ArmorStand.SetVisualItem))]
    public static class ArmorStand_SetVisualItem_ArmorStandAttachment
    {
        private static void Prefix(int index, List<ArmorStand.ArmorStandSlot> ___m_slots, string itemName, int variant, ref bool __state)
        {
            if (!visualStateArmorStand.Value)
                return;

            if (!CircletItem.IsCircletItemName(itemName))
                return;

            ArmorStand.ArmorStandSlot armorStandSlot = ___m_slots[index];

            if (armorStandSlot.m_slot != VisSlot.Helmet)
                return;

            if (armorStandSlot.m_visualName == itemName && armorStandSlot.m_visualVariant == variant)
                return;

            __state = true;
        }

        private static void Postfix(int index, VisEquipment ___m_visEquipment, List<ArmorStand.ArmorStandSlot> ___m_slots, bool __state)
        {
            if (!__state)
                return;

            ___m_visEquipment.UpdateVisuals();

            ArmorStand.ArmorStandSlot armorStandSlot = ___m_slots[index];
            if (!CircletItem.IsCircletItemName(armorStandSlot.m_visualName))
                return;

            GameObject visualItem = ___m_visEquipment.m_helmetItemInstance;
            if (visualItem == null)
                return;

            DvergerLightController component = visualItem.GetComponentInChildren<DvergerLightController>(includeInactive: true);
            if (component != null)
                Object.Destroy(component);

            Light[] lights = visualItem.GetComponentsInChildren<Light>(includeInactive: true);
            if (lights.Length == 0)
                return;

            visualItem.AddComponent<DvergerLightController>().Initialize(lights[0], zdoIndex:index);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupEquipment))]
    public static class Humanoid_SetupVisEquipment_AttachLayersFix
    {
        private static void Postfix(Humanoid __instance)
        {
            DvergerLightController.UpdateVisualsLayers(__instance.m_visual);
        }
    }

}
