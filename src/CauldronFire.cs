using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VikingFoodPlus
{
    /// <summary>
    /// Feuer unter dem Runenkessel. Verhaelt sich wie ein Lagerfeuer: Holz nachlegen (E oder Holz benutzen),
    /// brennt mit der Zeit herunter und geht aus. Solange es brennt, sind die Flammen und die Feuer-Effektzone
    /// aktiv, dadurch erkennt der Kessel wie in Vanilla, dass ein Feuer darunter brennt.
    /// Der Brennstoff wird im ZDO des Kessels gespeichert (Multiplayer-faehig).
    /// Hover-Text und E-Taste laufen ueber Valheims eigene Switch-Komponente (wie beim Lagerfeuer),
    /// damit der Code nicht von Aenderungen an den Interfaces Hoverable/Interactable abhaengt.
    /// </summary>
    public class CauldronFire : MonoBehaviour
    {
        private const string FuelKey = "vfp_fire_fuel";
        private const string TimeKey = "vfp_fire_time";
        private const string RpcAddFuel = "VFP_CauldronAddFuel";

        // Werte werden beim Registrieren vom Vanilla-Lagerfeuer (fire_pit) uebernommen.
        public float MaxFuel = 10f;
        public float SecPerFuel = 5000f;
        public float StartFuel = 0f;
        public string FuelItemName = "$item_wood";
        public EffectList FuelAddedEffects;
        public List<GameObject> BurningObjects = new List<GameObject>();

        private ZNetView nview;
        private float timer;
        private bool shown = true;
        private bool registered;

        private void Awake()
        {
            nview = GetComponentInParent<ZNetView>();
            SetBurning(false, true);
            HookSwitch();
        }

        /// <summary>Verbindet die Switch-Komponente (Hover-Text + E-Taste) mit diesem Feuer.</summary>
        private void HookSwitch()
        {
            var sw = GetComponent<Switch>();
            if (sw == null) sw = gameObject.AddComponent<Switch>();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            try
            {
                var nameField = typeof(Switch).GetField("m_name", flags);
                if (nameField != null && nameField.FieldType == typeof(string)) nameField.SetValue(sw, "$vfp_fire");
                var useField = typeof(Switch).GetField("m_onUse", flags);
                if (useField != null) useField.SetValue(sw, Delegate.CreateDelegate(useField.FieldType, this, GetType().GetMethod(nameof(OnSwitchUse))));
                var hoverField = typeof(Switch).GetField("m_onHover", flags);
                if (hoverField != null) hoverField.SetValue(sw, Delegate.CreateDelegate(hoverField.FieldType, this, GetType().GetMethod(nameof(OnSwitchHover))));
            }
            catch (Exception ex) { Debug.LogWarning("[Viking's Feast] Kesselfeuer: Switch konnte nicht verbunden werden: " + ex.Message); }
        }

        private void Start()
        {
            // Start statt Awake: hier hat der ZNetView des Kessels sein ZDO sicher schon.
            if (nview == null) nview = GetComponentInParent<ZNetView>();
            if (nview != null && nview.GetZDO() != null && !registered)
            {
                nview.Register(RpcAddFuel, RPC_AddFuel);
                registered = true;
            }
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = 1f;
            bool burning = false;
            if (nview != null && nview.IsValid())
            {
                if (nview.IsOwner()) UpdateFuel();
                burning = GetFuel() > 0f;
            }
            SetBurning(burning, false);
        }

        private float GetFuel()
        {
            if (nview == null || !nview.IsValid()) return 0f;
            float fuel = nview.GetZDO().GetFloat(FuelKey, -1f);
            return fuel < 0f ? StartFuel : fuel;
        }

        private void UpdateFuel()
        {
            var zdo = nview.GetZDO();
            long now = ZNet.instance.GetTime().Ticks;
            long last = zdo.GetLong(TimeKey, 0L);
            float fuel = GetFuel();
            if (last > 0L && now > last)
            {
                double seconds = (now - last) / 10000000.0;
                fuel -= (float)(seconds / Mathf.Max(1f, SecPerFuel));
            }
            fuel = Mathf.Clamp(fuel, 0f, MaxFuel);
            zdo.Set(FuelKey, fuel);
            zdo.Set(TimeKey, now);
        }

        private void RPC_AddFuel(long sender)
        {
            if (nview == null || !nview.IsValid() || !nview.IsOwner()) return;
            UpdateFuel();
            float fuel = GetFuel();
            nview.GetZDO().Set(FuelKey, Mathf.Clamp(fuel + 1f, 0f, MaxFuel));
            SetBurning(true, false);
        }

        private void SetBurning(bool burning, bool force)
        {
            if (!force && burning == shown) return;
            shown = burning;
            foreach (var go in BurningObjects)
                if (go != null) go.SetActive(burning);
        }

        // ---- Uebersetzung per Reflection (Valheims Localization liegt je nach Version in einer anderen DLL) ----
        private static Type locType;
        private static MethodInfo locSimple, locWords;

        private static string Loc(string text, params string[] words)
        {
            try
            {
                if (locType == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var t = asm.GetType("Localization", false);
                        if (t != null) { locType = t; break; }
                    }
                    if (locType == null) return text;
                    locSimple = locType.GetMethod("Localize", new[] { typeof(string) });
                    locWords = locType.GetMethod("Localize", new[] { typeof(string), typeof(string[]) });
                }
                const BindingFlags st = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                object instance = locType.GetProperty("instance", st)?.GetValue(null, null) ?? locType.GetField("instance", st)?.GetValue(null);
                if (instance == null) return text;
                if (words != null && words.Length > 0 && locWords != null) return (string)locWords.Invoke(instance, new object[] { text, words });
                if (locSimple != null) return (string)locSimple.Invoke(instance, new object[] { text });
            }
            catch (Exception) { }
            return text;
        }

        // ---- Switch-Callbacks (public, damit Delegate.CreateDelegate sie sicher findet) ----
        public string OnSwitchHover()
        {
            if (nview == null || !nview.IsValid()) return "";
            string text = "$vfp_fire ( $vfp_fire_fuel " + Mathf.CeilToInt(GetFuel()) + "/" + Mathf.RoundToInt(MaxFuel) + " )"
                + "\n[<color=yellow><b>$KEY_Use</b></color>] $vfp_fire_add";
            return Loc(text);
        }

        public bool OnSwitchUse(Switch caller, Humanoid user, ItemDrop.ItemData item)
        {
            if (item != null && (item.m_shared == null || item.m_shared.m_name != FuelItemName)) return false;
            return TryAddFuel(user);
        }

        private bool TryAddFuel(Humanoid user)
        {
            if (user == null) return false;
            if (nview == null || !nview.IsValid()) return false;
            if (GetFuel() > MaxFuel - 1f)
            {
                user.Message(MessageHud.MessageType.Center, Loc("$msg_cantaddmore"));
                return false;
            }
            var inventory = user.GetInventory();
            if (inventory == null || inventory.CountItems(FuelItemName) <= 0)
            {
                user.Message(MessageHud.MessageType.Center, Loc("$msg_outof " + FuelItemName));
                return false;
            }
            inventory.RemoveItem(FuelItemName, 1);
            nview.InvokeRPC(RpcAddFuel);
            try { if (FuelAddedEffects != null) FuelAddedEffects.Create(transform.position, transform.rotation); }
            catch (Exception) { }
            user.Message(MessageHud.MessageType.Center, Loc("$msg_fireadding", FuelItemName));
            return true;
        }
    }
}
