using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using BepInEx.Configuration;

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
        private float _minAngle = 30;
        private float _maxAngle = 110;
        private float _minIntensity = 1.4f;
        private float _maxIntensity = 2.2f;
        private float _minRange = 45;
        private float _maxRange = 15;
        private float _pointIntensity = 1.1f;
        private float _pointRange = 10;

        private bool forceOff = false;

        public int quality = 1;

        public LightState m_state = new LightState();

        const float intensityIncrement = 0.1f;
        const float intensityFactorMax = 3f;
        const float intensityFactorMin = 0.1f;

        private static int s_rayMaskSolids = 0;
        private static int s_rayMaskCharacters = 0;

        public class LightState
        {
            public bool on = true;
            public int level = 2;
            public float intensity = 1;
            public Color color = new Color(0.25f, 0.38f, 0.37f);
            public bool shadows = false;
            public bool spot = true;
        }

        private void Awake()
        {
            m_nview = GetComponentInParent<ZNetView>();

            if (m_nview.GetZDO() == null)
            {
                base.enabled = false;
                return;
            }
            
        }

        private void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null)
                return;

            GetSpotLight();

            if (player != null && player == m_playerAttached && player.TakeInput())
            {
                forceOff = CircletExtended.disableOnSleep.Value && player.InBed();

                if (!forceOff && StateChanged(player))
                    SaveState();
            }

            UpdateLights();
        }

        private bool StateChanged(Player player)
        {
            if (IsShortcutDown(CircletExtended.toggleShortcut.Value))
            {
                m_state.on = !m_state.on;
                CircletExtended.LogInfo("Toggle");
                return true;
            }
            else if (IsShortcutDown(CircletExtended.toggleSpotShortcut.Value))
            {
                m_state.spot = !m_state.spot;
                CircletExtended.LogInfo("Toggle spot");
                return true;
            }

            if (!m_state.on)
                return false;

            if (IsShortcutDown(CircletExtended.widenShortcut.Value) && m_state.level > 0)
            {
                m_state.level--;
                CircletExtended.LogInfo("Widen");
                return true;
            }
            else if (IsShortcutDown(CircletExtended.narrowShortcut.Value) && m_state.level < _maxLevel)
            {
                m_state.level++;
                CircletExtended.LogInfo("Narrow");
                return true;
            }
            else if (IsShortcutDown(CircletExtended.increaseIntensityShortcut.Value) && m_state.intensity < intensityFactorMax)
            {
                m_state.intensity += Mathf.Min(intensityIncrement, intensityFactorMax);
                CircletExtended.LogInfo("Increase intensity");
                return true;
            }
            else if (IsShortcutDown(CircletExtended.decreaseIntensityShortcut.Value) && m_state.intensity > intensityFactorMin)
            {
                m_state.intensity -= Mathf.Max(intensityIncrement, intensityFactorMin);
                CircletExtended.LogInfo("Decrease intensity");
                return true;
            }
            else if (IsShortcutDown(CircletExtended.toggleShadowsShortcut.Value))
            {
                m_state.shadows = !m_state.shadows;
                CircletExtended.LogInfo("Toggle shadows");
                return true;
            }
            else if (CircletExtended.enableOverload.Value && IsShortcutDown(CircletExtended.overloadShortcut.Value))
            {
                CircletExtended.LogInfo("Overload");
                ApplyOverloadEffect(player);
            }

            return false;
        }

        private void ApplyOverloadEffect(Player player)
        {
            if (CircletExtended.overloadEffect == null)
            {
                Incinerator incinerator = Resources.FindObjectsOfTypeAll<Incinerator>().FirstOrDefault();

                CircletExtended.overloadEffect = Instantiate(incinerator.m_lightingAOEs);
                CircletExtended.overloadEffect.name = "circletExtendedOverload";
                for (int i = CircletExtended.overloadEffect.transform.childCount - 1; i > 0; i--)
                {
                    Transform child = CircletExtended.overloadEffect.transform.GetChild(i);
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
                s_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
                s_rayMaskCharacters = LayerMask.GetMask("character", "character_net", "character_ghost", "character_noenv");
            }

            Instantiate(CircletExtended.overloadEffect, player.transform.position, m_frontLight.transform.rotation);

            float radius = (float)Math.Tan((m_frontLight.spotAngle / 2) * (Math.PI / 180)) * m_frontLight.range;

            RaycastHit[] array = Physics.SphereCastAll(m_frontLight.transform.position, radius, m_frontLight.transform.forward, m_frontLight.range, LayerMask.GetMask("character", "character_net", "character_ghost", "character_noenv"));
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

                    if (distance <= 5f || angleCheck && distance <= m_frontLight.range && !Physics.Linecast(m_frontLight.transform.position, charPos, LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle")))
                        character.AddStaggerDamage(character.GetStaggerTreshold(), vector.normalized);
                }
            }
        }

        public bool IsValidTarget(IDestructible destr)
        {
            Character character = destr as Character;
            if ((bool)character)
            {
                if (character == m_playerAttached)
                {
                    return false;
                }

                if (m_playerAttached != null)
                {
                    bool flag = BaseAI.IsEnemy(m_playerAttached, character) || ((bool)character.GetBaseAI() && character.GetBaseAI().IsAggravatable() && m_playerAttached.IsPlayer());
                    if (!m_playerAttached.IsPlayer() && !flag)
                    {
                        return false;
                    }

                    if (m_playerAttached.IsPlayer() && !m_playerAttached.IsPVPEnabled() && !flag)
                    {
                        return false;
                    }
                }

                if (character.IsDodgeInvincible())
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsShortcutDown(KeyboardShortcut value)
        {
            return value.IsDown() || Input.GetKeyDown(value.MainKey) && !value.Modifiers.Any();
        }

        private void ShowMessage()
        {
            if (MessageHud.instance != null && m_playerAttached != null && Player.m_localPlayer == m_playerAttached)
                if (!forceOff && m_state.on)
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, $"$item_helmet_dverger: $msg_level {m_state.level} ({Mathf.FloorToInt(m_state.intensity * 100)}%)");
                else
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "$item_helmet_dverger: $hud_off");
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
            if (!m_nview.IsValid())
                return;

            string stateJSON = "";

            if (m_item != null)
            {
                stateJSON = m_item.m_customData.GetValueSafe(CircletExtended.customDataKey);
                CircletExtended.LogInfo($"m_customData: {stateJSON}");
                quality = m_item.m_quality;
            }
            
            if (String.IsNullOrWhiteSpace(stateJSON))
            {
                ZDO zdo = m_nview.GetZDO();

                int @int = zdo.GetInt(ZDOVars.s_dataCount);
                for (int i = 0; i < @int; i++)
                    if (zdo.GetString($"data_{i}") == CircletExtended.customDataKey)
                        stateJSON = zdo.GetString($"data__{i}");

                quality = zdo.GetInt(ZDOVars.s_quality, 1);
                CircletExtended.LogInfo($"m_nview: {stateJSON}");
            }

            if (!String.IsNullOrWhiteSpace(stateJSON))
            {
                try
                { 
                    m_state = JsonUtility.FromJson<LightState>(stateJSON); 
                }
                catch (Exception e)
                {
                    CircletExtended.LogInfo($"State parsing error:\n{e}");
                }
            }

            SetQuality();

            ShowMessage();
        }

        private void SaveState()
        {
            if (!m_nview.IsValid())
                return;

            if (m_item == null)
                return;

            m_state.color = CircletExtended.circletColor.Value;

            m_item.m_customData[CircletExtended.customDataKey] = JsonUtility.ToJson(m_state);

            ShowMessage();
        }

        private void SetQuality()
        {
            _maxLevel = Mathf.Clamp(CircletExtended.MaxSteps.Value, 1, 50);
            _minAngle = Mathf.Clamp(CircletExtended.MinAngle.Value, 1, 360);
            _maxAngle = Mathf.Clamp(CircletExtended.MaxAngle.Value, 1, 360);
            _minIntensity = Mathf.Clamp(CircletExtended.MaxIntensity.Value, 0, 10);
            _maxIntensity = Mathf.Clamp(CircletExtended.MinIntensity.Value, 0, 10);
            _minRange = Mathf.Clamp(CircletExtended.MinRange.Value, 0, 1000);
            _maxRange = Mathf.Clamp(CircletExtended.MaxRange.Value, 0, 1000);
            _pointIntensity = Mathf.Clamp(CircletExtended.PointIntensity.Value, 0, 10);
            _pointRange = Mathf.Clamp(CircletExtended.PointRange.Value, 0, 1000);
        }

        private void UpdateLights()
        {
            if (m_state.level == _maxLevel)
            {
                m_frontLight.type = LightType.Point;
                m_frontLight.intensity = _pointIntensity * m_state.intensity;
                m_frontLight.range = _pointRange;
            }
            else
            {
                float t = (float) m_state.level / Mathf.Max(_maxLevel - 1, 1f);

                m_frontLight.type = LightType.Spot;
                m_frontLight.spotAngle = Mathf.Lerp(_minAngle, _maxAngle, t);
                m_frontLight.intensity = Mathf.Lerp(_minIntensity, _maxIntensity, t) * m_state.intensity;
                m_frontLight.range = Mathf.Lerp(_minRange, _maxRange, t);
            }

            m_frontLight.enabled = !forceOff && m_state.on;
            m_frontLight.shadows = CircletExtended.enableShadows.Value && m_state.shadows ? LightShadows.Soft : LightShadows.None;

            if (m_spotLight != null)
            {
                m_spotLight.enabled = !forceOff && m_state.spot;
                m_spotLight.shadows = m_frontLight.shadows;
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
            if (!CircletExtended.modEnabled.Value)
                return;

            if (itemHash != CircletExtended.itemHashHelmetDverger || !__instance.m_isPlayer || __result == null)
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

            if (player.m_helmetItem == null)
                return;

            if (player.m_helmetItem.m_dropPrefab.name != CircletExtended.itemNameHelmetDverger)
                return;

            __result.AddComponent<DvergerLightController>().Initialize(lights[0], player, player.m_helmetItem);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
    public static class ItemDrop_Start_Patch
    {
        private static void Postfix(ItemDrop __instance)
        {
            if (!CircletExtended.modEnabled.Value)
                return;

            if (__instance.GetPrefabName(__instance.name) != CircletExtended.itemNameHelmetDverger)
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
            if (!CircletExtended.modEnabled.Value)
                return;

            if (__instance.GetAttachedItem() != CircletExtended.itemNameHelmetDverger)
                return;

            if (___m_visualItem != null)
                return;

            if (___m_visualName == itemName && ___m_visualVariant == variant)
                return;

            __state = true;
        }

        private static void Postfix(ItemStand __instance, GameObject ___m_visualItem, bool __state)
        {
            if (!CircletExtended.modEnabled.Value)
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
    

}
