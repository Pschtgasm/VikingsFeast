using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VikingFoodPlus
{
    /// <summary>
    /// Runenkessel: Rezeptliste in zwei aufklappbare Gruppen "Essen" und "Met" teilen; Gerichte nach Gesundheit/Ausdauer/Eitr sortierbar.
    /// Nur am Runenkessel und nur im Herstellen-Reiter; an allen anderen Stationen bleibt die Liste wie in Valheim.
    /// Der Auf-/Zu-Zustand gilt bis zum Neustart des Spiels.
    /// </summary>
    public static class RecipeGroups
    {
        internal static bool FoodOpen = true, MeadOpen = true, SecretOpen = true;
        /// <summary>Geheimrezepte: Item-Prefabname -> (Namens-Schluessel, Hinweis-Schluessel). Leer = keine Geheimrezepte.</summary>
        internal static readonly List<string[]> SecretItems = new List<string[]>();
        /// <summary>true = Geheimrezepte muessen per Rolle gelernt werden (sonst wie normale Rezepte).</summary>
        internal static bool SecretsNeedScroll = true;
        /// <summary>Gerade angeklickte ???-Zeile (Item-Prefabname) oder null.</summary>
        internal static string LockedSel;
        private static string[] lockedInfo;
        private static readonly FieldInfo SelectedField = AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe");
        private static readonly FieldInfo NameField = AccessTools.Field(typeof(InventoryGui), "m_recipeName");
        private static readonly FieldInfo DescField = AccessTools.Field(typeof(InventoryGui), "m_recipeDecription");

        /// <summary>???-Zeile angeklickt: Auswahl aufheben, rechts stattdessen den Hinweis zur Rolle zeigen (ohne das Gericht zu verraten).</summary>
        private static void SelectLocked(InventoryGui gui, string[] info, GameObject row)
        {
            LockedSel = info[0]; lockedInfo = info;
            try
            {
                if (SelectedField != null)
                    SelectedField.SetValue(gui, SelectedField.FieldType.IsValueType ? Activator.CreateInstance(SelectedField.FieldType) : null);
                // Markierung der bisher gewaehlten Rezeptzeile entfernen, ???-Zeilen umfaerben
                var list = AvailableField != null ? AvailableField.GetValue(gui) as IList : null;
                if (list != null)
                    foreach (var pair in list)
                    {
                        var ep = pair.GetType().GetProperty("InterfaceElement");
                        var el = ep != null ? ep.GetValue(pair, null) as GameObject : null;
                        var selT = el != null ? el.transform.Find("selected") : null;
                        if (selT != null) selT.gameObject.SetActive(false);
                    }
                foreach (var h in headers)
                {
                    if (h == null) continue;
                    var t = h.transform.Find("name"); var g = t != null ? t.GetComponent<Graphic>() : null;
                    if (g != null && h.name == "VFP_LockedRow") g.color = h == row ? new Color(1f, 0.82f, 0.45f) : new Color(0.62f, 0.56f, 0.48f);
                }
            }
            catch (Exception) { }
        }

        private static void SetText(object tmp, string text)
        {
            if (tmp == null) return;
            var pt = tmp.GetType().GetProperty("text", typeof(string));
            if (pt != null && pt.CanWrite) pt.SetValue(tmp, text, null);
            var b = tmp as Behaviour; if (b != null) b.enabled = true;
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
        public static class LockedDetails
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (LockedSel == null || lockedInfo == null || SelectedField == null) return;
                try
                {
                    var sel = SelectedField.GetValue(__instance);
                    var rp = sel != null ? sel.GetType().GetProperty("Recipe") : null;
                    var rec = rp != null ? rp.GetValue(sel, null) as Recipe : null;
                    if (rec != null || !__instance.InCraftTab()) { LockedSel = null; lockedInfo = null; return; }   // normales Rezept gewaehlt
                    string title = CauldronFire.Loc("$vfp_secret_locked");
                    if (string.IsNullOrEmpty(title) || title.StartsWith("[")) title = "??? – Geheimrezept";
                    string hint = CauldronFire.Loc(lockedInfo[2]);
                    string how = CauldronFire.Loc("$vfp_secret_locked_desc");
                    if (string.IsNullOrEmpty(how) || how.StartsWith("[")) how = "Finde die passende Rezeptrolle, um dieses Geheimrezept zu lernen.";
                    SetText(NameField != null ? NameField.GetValue(__instance) : null, title);
                    SetText(DescField != null ? DescField.GetValue(__instance) : null, "<color=#ffd27a>" + hint + "</color>\n\n" + how);
                    if (__instance.m_recipeIcon != null && lockedInfo.Length > 3 && ObjectDB.instance != null)
                    {
                        var sc = ObjectDB.instance.GetItemPrefab(lockedInfo[3]);
                        var drop = sc != null ? sc.GetComponent<ItemDrop>() : null;
                        if (drop != null) { __instance.m_recipeIcon.sprite = drop.m_itemData.GetIcon(); __instance.m_recipeIcon.enabled = true; }
                    }
                }
                catch (Exception) { }
            }
        }
        internal static string FoodLabel = "Essen", MeadLabel = "Met";
        /// <summary>Gruppen Essen/Met am Runenkessel (Config Drinks > RecipeGroups, nur mit Getraenken).</summary>
        internal static bool GroupsEnabled = true;
        /// <summary>Wo die Sortier-Knoepfe erscheinen: Off, RuneCauldron, Cooking, All (Config General > SortButtons).</summary>
        internal static string SortScope = "RuneCauldron";
        /// <summary>Sortierung der Gerichte: 0 = wie Valheim, 1 = Gesundheit, 2 = Ausdauer, 3 = Eitr (jeweils absteigend).</summary>
        internal static int SortMode = 0;
        private static readonly List<GameObject> headers = new List<GameObject>();
        private static GameObject sortBar;
        private static Image[] sortImages;
        private static GameObject[] sortLabels;

        private static readonly FieldInfo AvailableField = AccessTools.Field(typeof(InventoryGui), "m_availableRecipes");
        private static readonly FieldInfo BaseSizeField = AccessTools.Field(typeof(InventoryGui), "m_recipeListBaseSize");
        private static readonly MethodInfo UpdatePanel = AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) });

        private static bool IsSecret(Recipe r)
        {
            var n = r != null && r.m_item != null ? r.m_item.name : "";
            return n.StartsWith("VFP_Secret_");
        }

        private static bool IsMead(Recipe r)
        {
            var n = r != null && r.m_item != null ? r.m_item.name : "";
            return n.StartsWith("VFP_MeadBase_") || n.StartsWith("VFP_KegBase_");
        }

        private static CraftingStation CurrentStation()
        {
            var p = Player.m_localPlayer;
            return p != null ? p.GetCurrentCraftingStation() : null;
        }

        private static bool IsRuneCauldron(CraftingStation cs)
        {
            return cs != null && (cs.m_name == "$vfp_station" || cs.gameObject.name.StartsWith("VFP_RuneCauldron"));
        }

        private static bool IsCooking(CraftingStation cs)
        {
            if (cs == null) return false;
            if (IsRuneCauldron(cs)) return true;
            string go = cs.gameObject.name, n = cs.m_name ?? "";
            foreach (var id in new[] { "piece_cauldron", "piece_preptable", "piece_oven" })
                if (go.StartsWith(id) || n == "$" + id) return true;
            return false;
        }

        private static bool SortHere(CraftingStation cs)
        {
            switch (SortScope)
            {
                case "RuneCauldron": return IsRuneCauldron(cs);
                case "Cooking": return IsCooking(cs);
                case "All": return cs != null;
                default: return false;
            }
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
            if (gui == null) return;
            var cs = CurrentStation();
            bool craftTab = gui.InCraftTab();
            bool sortHere = craftTab && SortHere(cs);
            try { UpdateSortBar(gui, sortHere); } catch (Exception) { }
            bool groups = craftTab && GroupsEnabled && IsRuneCauldron(cs);
            bool sorting = sortHere && SortMode > 0;
            if ((!groups && !sorting) || AvailableField == null) return;
            var list = AvailableField.GetValue(gui) as IList;
            if (list == null || list.Count == 0) return;
            var food = new List<object>(); var mead = new List<object>(); var secret = new List<object>();
            PropertyInfo recipeProp = null, elemProp = null;
            foreach (var pair in list)
            {
                if (recipeProp == null)
                {
                    recipeProp = pair.GetType().GetProperty("Recipe");
                    elemProp = pair.GetType().GetProperty("InterfaceElement");
                    if (recipeProp == null || elemProp == null) return;
                }
                var rcp = recipeProp.GetValue(pair, null) as Recipe;
                if (groups && IsMead(rcp)) mead.Add(pair);
                else if (groups && IsSecret(rcp)) secret.Add(pair);
                else food.Add(pair);
            }
            if (sorting)
            {
                var rp = recipeProp;
                Comparison<object> cmp = (a, b) =>
                {
                    int c = FoodStat(rp.GetValue(b, null) as Recipe).CompareTo(FoodStat(rp.GetValue(a, null) as Recipe));
                    return c != 0 ? c : string.Compare(RecipeName(rp.GetValue(a, null) as Recipe), RecipeName(rp.GetValue(b, null) as Recipe), StringComparison.Ordinal);
                };
                food.Sort(cmp); secret.Sort(cmp);
            }
            float space = gui.m_recipeListSpace;
            int row = 0;
            list.Clear();
            if (!groups)
            {
                // Andere Station: nur sortieren, keine Gruppen
                foreach (var p in food) { list.Add(p); Place(elemProp.GetValue(p, null) as GameObject, row++, space); }
            }
            else
            {
                // Liste neu aufbauen: zugeklappte Gruppen entfernen (Elemente zerstoeren)
                foreach (var p in food)
                    if (FoodOpen) list.Add(p); else UnityEngine.Object.Destroy(elemProp.GetValue(p, null) as GameObject);
                foreach (var p in secret)
                    if (SecretOpen) list.Add(p); else UnityEngine.Object.Destroy(elemProp.GetValue(p, null) as GameObject);
                foreach (var p in mead)
                    if (MeadOpen) list.Add(p); else UnityEngine.Object.Destroy(elemProp.GetValue(p, null) as GameObject);

                string foodLabel = CauldronFire.Loc("$vfp_group_food"), meadLabel = CauldronFire.Loc("$vfp_group_mead");
                if (string.IsNullOrEmpty(foodLabel) || foodLabel.StartsWith("[") || foodLabel.StartsWith("$")) foodLabel = FoodLabel;
                if (string.IsNullOrEmpty(meadLabel) || meadLabel.StartsWith("[") || meadLabel.StartsWith("$")) meadLabel = MeadLabel;
                headers.Add(MakeHeader(gui, (FoodOpen ? "▼ " : "▶ ") + foodLabel + " (" + food.Count + ")", row++, () => { FoodOpen = !FoodOpen; Refresh(gui); }));
                if (FoodOpen) foreach (var p in food) Place(elemProp.GetValue(p, null) as GameObject, row++, space);
                // Geheimrezepte: gelernte als normale Zeilen, noch nicht gelernte als "???"-Zeile mit Hinweis auf den Fundort der Rolle
                if (SecretItems.Count > 0)
                {
                    var locked = new List<string[]>();
                    var pl = Player.m_localPlayer;
                    if (SecretsNeedScroll && pl != null)
                        foreach (var si in SecretItems)
                        {
                            bool shown = false;
                            foreach (var p in secret) { var rr = recipeProp.GetValue(p, null) as Recipe; if (rr != null && rr.m_item != null && rr.m_item.name == si[0]) { shown = true; break; } }
                            if (!shown && !pl.IsRecipeKnown(si[1])) locked.Add(si);
                        }
                    if (secret.Count > 0 || locked.Count > 0)
                    {
                        string secLabel = CauldronFire.Loc("$vfp_group_secret");
                        if (string.IsNullOrEmpty(secLabel) || secLabel.StartsWith("[") || secLabel.StartsWith("$")) secLabel = "Geheimrezepte";
                        int total = SecretItems.Count, known = total - locked.Count;
                        headers.Add(MakeHeader(gui, (SecretOpen ? "▼ " : "▶ ") + secLabel + " (" + known + "/" + total + ")", row++, () => { SecretOpen = !SecretOpen; Refresh(gui); }));
                        if (SecretOpen)
                        {
                            foreach (var p in secret) Place(elemProp.GetValue(p, null) as GameObject, row++, space);
                            foreach (var si in locked)
                            {
                                var info = si;
                                GameObject lk = null;
                                lk = MakeHeader(gui, "   ???", row++, () => SelectLocked(gui, info, lk));
                                var nm = lk.transform.Find("name");
                                var g2 = nm != null ? nm.GetComponent<Graphic>() : null;
                                if (g2 != null) g2.color = LockedSel == info[0] ? new Color(1f, 0.82f, 0.45f) : new Color(0.62f, 0.56f, 0.48f);
                                lk.name = "VFP_LockedRow";
                                headers.Add(lk);
                            }
                        }
                    }
                }
                if (mead.Count > 0)
                {
                    headers.Add(MakeHeader(gui, (MeadOpen ? "▼ " : "▶ ") + meadLabel + " (" + mead.Count + ")", row++, () => { MeadOpen = !MeadOpen; Refresh(gui); }));
                    if (MeadOpen) foreach (var p in mead) Place(elemProp.GetValue(p, null) as GameObject, row++, space);
                }
            }
            float baseSize = BaseSizeField != null ? (float)BaseSizeField.GetValue(gui) : 0f;
            gui.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(baseSize, row * space));
        }

        private static float FoodStat(Recipe r)
        {
            var d = r != null && r.m_item != null ? r.m_item.m_itemData : null;
            if (d == null || d.m_shared == null) return 0f;
            var sh = d.m_shared;
            return SortMode == 1 ? sh.m_food : SortMode == 2 ? sh.m_foodStamina : SortMode == 3 ? sh.m_foodEitr : 0f;
        }

        private static string RecipeName(Recipe r)
        {
            return r != null && r.m_item != null ? CauldronFire.Loc(r.m_item.m_itemData.m_shared.m_name) ?? "" : "";
        }

        /// <summary>Drei Valheim-Traenke oben rechts im Herstellmenue, auf Hoehe der Reiter Herstellen/Aufwerten:
        /// Gesundheits-, Ausdauer- und Eitr-Trank. Erneutes Klicken auf den aktiven Trank stellt die normale Reihenfolge wieder her.</summary>
        private static void UpdateSortBar(InventoryGui gui, bool show)
        {
            if (!show) { if (sortBar != null) sortBar.SetActive(false); return; }
            var panel = gui.m_crafting;
            if (panel == null || gui.m_tabCraft == null) return;
            if (sortBar == null)
            {
                sortBar = new GameObject("VFP_SortBar", typeof(RectTransform));
                sortBar.transform.SetParent(panel, false);
                sortImages = new Image[3];
                sortLabels = new GameObject[3];
                string[] keys = { "$vfp_sort_health", "$vfp_sort_stamina", "$vfp_sort_eitr" };
                string[] fallbackText = { "Health", "Stamina", "Eitr" };
                string[] items = { "MeadHealthMinor", "MeadStaminaMinor", "MeadEitrMinor" };
                string[] fallback = { "Sort_Health", "Sort_Stamina", "Sort_Eitr" };
                for (int i = 0; i < 3; i++)
                {
                    int mode = i + 1;
                    var go = new GameObject("VFP_Sort" + mode, typeof(RectTransform), typeof(Image), typeof(Button));
                    go.transform.SetParent(sortBar.transform, false);
                    var img = go.GetComponent<Image>();
                    img.sprite = ItemIcon(items[i]) ?? LoadIcon(fallback[i]);
                    img.preserveAspect = true;
                    var b = go.GetComponent<Button>();
                    b.targetGraphic = img;
                    b.onClick.AddListener(() => { SortMode = SortMode == mode ? 0 : mode; Refresh(gui); });
                    sortImages[i] = img;
                    string label = CauldronFire.Loc(keys[i]);
                    if (string.IsNullOrEmpty(label) || label.StartsWith("[") || label.StartsWith("$")) label = fallbackText[i];
                    try
                    {
                        sortLabels[i] = MakeLabel(gui, go.transform, label);
                        if (sortLabels[i] != null)
                        {
                            sortLabels[i].SetActive(false);   // nur beim Drueberfahren mit der Maus sichtbar
                            go.AddComponent<SortHover>().Label = sortLabels[i];
                        }
                    }
                    catch (Exception) { }
                }
            }
            sortBar.SetActive(true);
            sortBar.transform.SetAsLastSibling();
            // Groesse und Hoehe an den Reiter "Herstellen" anlehnen, rechts buendig im Panel
            var tab = gui.m_tabCraft.transform as RectTransform;
            float size = tab != null ? Mathf.Clamp(tab.rect.height, 24f, 48f) : 32f;
            float y = tab != null ? panel.InverseTransformPoint(tab.position).y - panel.rect.yMax : -60f;
            var rt = (RectTransform)sortBar.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            float step = size + 4f;
            rt.sizeDelta = new Vector2(2 * step + size, size);
            rt.anchoredPosition = new Vector2(-22f, y);
            for (int i = 0; i < 3; i++)
            {
                var irt = (RectTransform)sortImages[i].transform;
                irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
                irt.pivot = new Vector2(0f, 0.5f);
                irt.sizeDelta = new Vector2(size, size);
                irt.anchoredPosition = new Vector2(i * step, 0f);
                sortImages[i].color = SortMode == i + 1 ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.6f);
                if (sortLabels[i] != null)
                {
                    var lrt = sortLabels[i].transform as RectTransform;
                    if (lrt != null)
                    {
                        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 1f);
                        lrt.pivot = new Vector2(0.5f, 0f);
                        lrt.sizeDelta = new Vector2(120f, 16f);
                        lrt.anchoredPosition = new Vector2(0f, 1f);
                    }
                    var g = sortLabels[i].GetComponent<Graphic>();
                    if (g != null) g.color = SortMode == i + 1 ? new Color(1f, 0.82f, 0.45f) : new Color(0.85f, 0.78f, 0.65f, 0.8f);
                }
            }
        }

        /// <summary>Kleine Beschriftung ueber einem Trank. Nutzt die Schrift der Rezeptliste (Valheim-Font).</summary>
        private static GameObject MakeLabel(InventoryGui gui, Transform parent, string text)
        {
            var src = gui.m_recipeElementPrefab != null ? gui.m_recipeElementPrefab.transform.Find("name") : null;
            if (src == null) return null;
            var go = UnityEngine.Object.Instantiate(src.gameObject, parent, false);
            go.name = "VFP_SortLabel";
            go.SetActive(true);
            foreach (Transform c in go.transform) UnityEngine.Object.Destroy(c.gameObject);
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                var t = c.GetType();
                var pt = t.GetProperty("text", typeof(string));
                if (pt == null || !pt.CanWrite) continue;
                pt.SetValue(c, text, null);
                var fs = t.GetProperty("fontSize");
                if (fs != null && fs.CanWrite)
                {
                    if (fs.PropertyType == typeof(float)) fs.SetValue(c, 13f, null);
                    else if (fs.PropertyType == typeof(int)) fs.SetValue(c, 13, null);
                }
                foreach (var autoName in new[] { "enableAutoSizing", "resizeTextForBestFit" })
                {
                    var au = t.GetProperty(autoName, typeof(bool));
                    if (au != null && au.CanWrite) au.SetValue(c, false, null);
                }
                var wrap = t.GetProperty("enableWordWrapping", typeof(bool));
                if (wrap != null && wrap.CanWrite) wrap.SetValue(c, false, null);
                var al = t.GetProperty("alignment");
                if (al != null && al.CanWrite && al.PropertyType.IsEnum)
                {
                    foreach (var name in new[] { "Center", "MiddleCenter", "Bottom", "LowerCenter" })
                        if (Enum.IsDefined(al.PropertyType, name)) { al.SetValue(c, Enum.Parse(al.PropertyType, name), null); break; }
                }
                break;
            }
            var g = go.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = false;   // Klicks gehen weiter an den Trank
            return go;
        }

        private static Sprite ItemIcon(string prefab)
        {
            try
            {
                var go = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
                var drop = go != null ? go.GetComponent<ItemDrop>() : null;
                return drop != null ? drop.m_itemData.GetIcon() : null;
            }
            catch (Exception) { return null; }
        }

        private static Sprite LoadIcon(string id)
        {
            try
            {
                using (var stream = typeof(RecipeGroups).Assembly.GetManifestResourceStream("VikingFoodPlus.Icons." + id + ".png"))
                {
                    if (stream == null) return null;
                    var bytes = new byte[stream.Length];
                    int off = 0, n;
                    while (off < bytes.Length && (n = stream.Read(bytes, off, bytes.Length - off)) > 0) off += n;
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(tex, bytes)) return null;
                    tex.name = "VFP_" + id;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                }
            }
            catch (Exception) { return null; }
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

    /// <summary>Zeigt die Beschriftung eines Sortier-Tranks nur, solange die Maus darueber ist.</summary>
    public class SortHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject Label;
        public void OnPointerEnter(PointerEventData e) { if (Label != null) Label.SetActive(true); }
        public void OnPointerExit(PointerEventData e) { if (Label != null) Label.SetActive(false); }
        private void OnDisable() { if (Label != null) Label.SetActive(false); }
    }
}
