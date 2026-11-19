using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace VikingFoodPlus
{
    /// <summary>
    /// Runenkessel: Rezeptliste in zwei aufklappbare Gruppen "Essen" und "Met" teilen.
    /// Nur am Runenkessel und nur im Herstellen-Reiter; an allen anderen Stationen bleibt die Liste wie in Valheim.
    /// Der Auf-/Zu-Zustand gilt bis zum Neustart des Spiels.
    /// </summary>
    public static class RecipeGroups
    {
        internal static bool FoodOpen = true, MeadOpen = true;
        internal static string FoodLabel = "Essen", MeadLabel = "Met";
        private static readonly List<GameObject> headers = new List<GameObject>();

        private static readonly FieldInfo AvailableField = AccessTools.Field(typeof(InventoryGui), "m_availableRecipes");
        private static readonly FieldInfo BaseSizeField = AccessTools.Field(typeof(InventoryGui), "m_recipeListBaseSize");
        private static readonly MethodInfo UpdatePanel = AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) });

        private static bool IsMead(Recipe r)
        {
            var n = r != null && r.m_item != null ? r.m_item.name : "";
            return n.StartsWith("VFP_MeadBase_") || n.StartsWith("VFP_KegBase_");
        }

        private static bool AtRuneCauldron()
        {
            var p = Player.m_localPlayer;
            var cs = p != null ? p.GetCurrentCraftingStation() : null;
            return cs != null && (cs.m_name == "$vfp_station" || cs.gameObject.name.StartsWith("VFP_RuneCauldron"));
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList")]
        public static class Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                foreach (var h in headers) if (h != null) UnityEngine.Object.Destroy(h);
                headers.Clear();
                try { Apply(__instance); }
                catch (Exception) { /* Bei Problemen bleibt die normale Valheim-Liste */ }
            }
        }

        private static void Apply(InventoryGui gui)
        {
            if (gui == null || !gui.InCraftTab() || !AtRuneCauldron() || AvailableField == null) return;
            var list = AvailableField.GetValue(gui) as IList;
            if (list == null || list.Count == 0) return;
            var food = new List<object>(); var mead = new List<object>();
            PropertyInfo recipeProp = null, elemProp = null;
            foreach (var pair in list)
            {
                if (recipeProp == null)
                {
                    recipeProp = pair.GetType().GetProperty("Recipe");
                    elemProp = pair.GetType().GetProperty("InterfaceElement");
                    if (recipeProp == null || elemProp == null) return;
                }
                if (IsMead(recipeProp.GetValue(pair, null) as Recipe)) mead.Add(pair); else food.Add(pair);
            }
            if (mead.Count == 0) return;   // ohne Met keine Gruppen noetig

            // Liste neu aufbauen: zugeklappte Gruppen entfernen (Elemente zerstoeren)
            list.Clear();
            foreach (var p in food)
                if (FoodOpen) list.Add(p); else UnityEngine.Object.Destroy(elemProp.GetValue(p, null) as GameObject);
            foreach (var p in mead)
                if (MeadOpen) list.Add(p); else UnityEngine.Object.Destroy(elemProp.GetValue(p, null) as GameObject);

            float space = gui.m_recipeListSpace;
            int row = 0;
            string foodLabel = CauldronFire.Loc("$vfp_group_food"), meadLabel = CauldronFire.Loc("$vfp_group_mead");
            if (string.IsNullOrEmpty(foodLabel) || foodLabel.StartsWith("[") || foodLabel.StartsWith("$")) foodLabel = FoodLabel;
            if (string.IsNullOrEmpty(meadLabel) || meadLabel.StartsWith("[") || meadLabel.StartsWith("$")) meadLabel = MeadLabel;
            headers.Add(MakeHeader(gui, (FoodOpen ? "▼ " : "▶ ") + foodLabel + " (" + food.Count + ")", row++, () => { FoodOpen = !FoodOpen; Refresh(gui); }));
            if (FoodOpen) foreach (var p in food) Place(elemProp.GetValue(p, null) as GameObject, row++, space);
            headers.Add(MakeHeader(gui, (MeadOpen ? "▼ " : "▶ ") + meadLabel + " (" + mead.Count + ")", row++, () => { MeadOpen = !MeadOpen; Refresh(gui); }));
            if (MeadOpen) foreach (var p in mead) Place(elemProp.GetValue(p, null) as GameObject, row++, space);

            float baseSize = BaseSizeField != null ? (float)BaseSizeField.GetValue(gui) : 0f;
            gui.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(baseSize, row * space));
        }

        private static void Place(GameObject element, int row, float space)
        {
            if (element == null) return;
            var rt = element.transform as RectTransform;
            if (rt != null) rt.anchoredPosition = new Vector2(0f, -row * space);
        }

        private static GameObject MakeHeader(InventoryGui gui, string text, int row, Action onClick)
        {
            var go = UnityEngine.Object.Instantiate(gui.m_recipeElementPrefab, gui.m_recipeListRoot);
            go.name = "VFP_RecipeGroupHeader";
            go.SetActive(true);
            Place(go, row, gui.m_recipeListSpace);
            var icon = go.transform.Find("icon");
            if (icon != null) { var img = icon.GetComponent<Image>(); if (img != null) img.enabled = false; }
            foreach (var hide in new[] { "Durability", "QualityLevel", "selected" })
            {
                var t = go.transform.Find(hide);
                if (t != null) t.gameObject.SetActive(false);
            }
            var name = go.transform.Find("name");
            if (name != null)
            {
                foreach (var c in name.GetComponents<Component>())
                {
                    var prop = c != null ? c.GetType().GetProperty("text", typeof(string)) : null;
                    if (prop != null && prop.CanWrite) { prop.SetValue(c, text, null); break; }
                }
                var g = name.GetComponent<Graphic>();
                if (g != null) g.color = new Color(1f, 0.82f, 0.45f);
            }
            var button = go.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick());
            }
            return go;
        }

        private static void Refresh(InventoryGui gui)
        {
            try { if (UpdatePanel != null && gui != null) UpdatePanel.Invoke(gui, new object[] { false }); }
            catch (Exception) { }
        }
    }
}
