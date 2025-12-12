using MorvaridEssential;

#if UNITY_EDITOR
namespace MorvaridEssential.Editor
{
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public static class UIAnimAddToPanel
    {
        // ==== Hierarchy menu ====
        [MenuItem("GameObject/UI Anim/Add Selected To Nearest Panel", false, 0)]
        public static void AddSelectedToNearestPanel() => AddSelectedCore();

        [MenuItem("GameObject/UI Anim/Remove Selected From Nearest Panel", false, 1)]
        public static void RemoveSelectedFromNearestPanel() => RemoveSelectedCore();

        // ==== Context menu on RectTransform ====
        [MenuItem("CONTEXT/RectTransform/Add To Nearest UI Anim Panel")]
        private static void ContextAdd(MenuCommand cmd) => AddOne((cmd.context as RectTransform)?.gameObject);

        [MenuItem("CONTEXT/RectTransform/Remove From Nearest UI Anim Panel")]
        private static void ContextRemove(MenuCommand cmd) => RemoveOne((cmd.context as RectTransform)?.gameObject);

        // ==== Core ====
        static void AddSelectedCore()
        {
            var objs = Selection.gameObjects;
            if (objs == null || objs.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select one or more objects in the Hierarchy.",
                    "OK");
                return;
            }

            int added = 0, skipped = 0, noPanel = 0, noRect = 0;

            Undo.IncrementCurrentGroup();
            foreach (var go in objs)
            {
                var rt = go ? go.GetComponent<RectTransform>() : null;
                if (!rt)
                {
                    noRect++;
                    continue;
                }

                var panel = rt.GetComponentInParent<Animalo>(true);
                if (!panel)
                {
                    noPanel++;
                    continue;
                }

                if (AddItem(panel, rt)) added++;
                else skipped++;
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            EditorUtility.DisplayDialog(
                "Add To Panel",
                $"Added: {added}\nSkipped (duplicate): {skipped}\nNo panel found: {noPanel}\nNo RectTransform: {noRect}",
                "OK"
            );
        }

        static void RemoveSelectedCore()
        {
            var objs = Selection.gameObjects;
            if (objs == null || objs.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select one or more objects in the Hierarchy.",
                    "OK");
                return;
            }

            int removed = 0, notFound = 0, noPanel = 0, noRect = 0;

            Undo.IncrementCurrentGroup();
            foreach (var go in objs)
            {
                var rt = go ? go.GetComponent<RectTransform>() : null;
                if (!rt)
                {
                    noRect++;
                    continue;
                }

                var panel = rt.GetComponentInParent<Animalo>(true);
                if (!panel)
                {
                    noPanel++;
                    continue;
                }

                if (RemoveItem(panel, rt)) removed++;
                else notFound++;
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            EditorUtility.DisplayDialog(
                "Remove From Panel",
                $"Removed: {removed}\nNot found in list: {notFound}\nNo panel found: {noPanel}\nNo RectTransform: {noRect}",
                "OK"
            );
        }

        static void AddOne(GameObject go)
        {
            if (!go) return;
            var rt = go.GetComponent<RectTransform>();
            if (!rt)
            {
                EditorUtility.DisplayDialog("Error", "Selected object does not have a RectTransform.", "OK");
                return;
            }

            var panel = rt.GetComponentInParent<Animalo>(true);
            if (!panel)
            {
                EditorUtility.DisplayDialog("No Panel", "No parent with MenuItemsAnimatorSODriven found.", "OK");
                return;
            }

            if (AddItem(panel, rt))
                EditorGUIUtility.PingObject(panel);
            else
                EditorUtility.DisplayDialog("Duplicate", "This target is already in the panel's item list.", "OK");
        }

        static void RemoveOne(GameObject go)
        {
            if (!go) return;
            var rt = go.GetComponent<RectTransform>();
            if (!rt)
            {
                EditorUtility.DisplayDialog("Error", "Selected object does not have a RectTransform.", "OK");
                return;
            }

            var panel = rt.GetComponentInParent<Animalo>(true);
            if (!panel)
            {
                EditorUtility.DisplayDialog("No Panel", "No parent with MenuItemsAnimatorSODriven found.", "OK");
                return;
            }

            if (RemoveItem(panel, rt))
                EditorGUIUtility.PingObject(panel);
            else
                EditorUtility.DisplayDialog("Not Found", "This target is not in the panel's item list.", "OK");
        }

        // ==== Helpers ====
        static bool AddItem(Animalo panel, RectTransform target)
        {
            if (panel.items != null && panel.items.Any(it => it != null && it.target == target))
                return false;

            Undo.RecordObject(panel, "Add Item To Panel");

            var list = (panel.items ?? new Animalo.Item[0]).ToList();
            var newItem = new Animalo.Item
            {
                target = target,
                delay = list.Count * panel.defaultDelay,
                action = null // Pick later in the Inspector
            };
            list.Add(newItem);
            panel.items = list.ToArray();

            EditorUtility.SetDirty(panel);
            return true;
        }

        static bool RemoveItem(Animalo panel, RectTransform target)
        {
            if (panel.items == null || panel.items.Length == 0) return false;

            var list = panel.items.ToList();
            int idx = list.FindIndex(it => it != null && it.target == target);
            if (idx < 0) return false;

            Undo.RecordObject(panel, "Remove Item From Panel");
            list.RemoveAt(idx);
            panel.items = list.ToArray();
            EditorUtility.SetDirty(panel);
            return true;
        }
    }
}
#endif