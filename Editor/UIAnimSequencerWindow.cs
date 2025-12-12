using MorvaridEssential;

#if UNITY_EDITOR
namespace MorvaridEssential.Editor
{
    // Assets/Editor/UIAnimSequencerWindow.cs
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UIAnimSequencerWindow : EditorWindow
{
    Animalo panel;
    Vector2 scroll;
    float grid = 0.05f;       // snap grid in seconds
    float nudge = 0.05f;      // nudge step
    bool[] selection;         // row selection
    bool ripple = false;      // ripple shifts items after the earliest selected
    GUIStyle header;

    [MenuItem("Tools/UI Anim/Sequencer")]
    public static void Open()
    {
        var win = GetWindow<UIAnimSequencerWindow>("UI Anim Sequencer");
        win.minSize = new Vector2(520, 260);
        win.Focus();
    }

    void OnEnable()
    {
        header = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };
        TryAutoPickPanel();
    }

    void TryAutoPickPanel()
    {
        if (panel) return;
        var sel = Selection.activeGameObject;
        if (!sel) return;
        panel = sel.GetComponentInParent<Animalo>(true);
        EnsureSelectionArray();
    }

    void EnsureSelectionArray()
    {
        var count = panel && panel.items != null ? panel.items.Length : 0;
        if (selection == null || selection.Length != count)
            selection = new bool[count];
    }

    void OnGUI()
    {
        DrawPanelPicker();

        if (!panel)
        {
            EditorGUILayout.HelpBox("Pick a panel (MenuItemsAnimatorSODriven) to edit timing.", MessageType.Info);
            return;
        }
        if (panel.items == null || panel.items.Length == 0)
        {
            EditorGUILayout.HelpBox("This panel has no items.", MessageType.Warning);
            return;
        }

        EnsureSelectionArray();
        DrawToolbar();
        DrawList();
        DrawFooter();
    }

    void DrawPanelPicker()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Panel", header, GUILayout.Width(60));
        panel = (Animalo)EditorGUILayout.ObjectField(panel, typeof(Animalo), true);
        if (GUILayout.Button("Pick From Selection", GUILayout.Width(150)))
            TryAutoPickPanel();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        ripple = GUILayout.Toggle(ripple, new GUIContent("Ripple"), GUILayout.Width(70));
        grid = EditorGUILayout.FloatField(new GUIContent("Grid"), grid, GUILayout.Width(140));
        nudge = EditorGUILayout.FloatField(new GUIContent("Nudge"), nudge, GUILayout.Width(160));

        if (GUILayout.Button("◄ Nudge", GUILayout.Width(90)))
            ApplyToSelected(d => d - nudge, ripple);

        if (GUILayout.Button("Nudge ►", GUILayout.Width(90)))
            ApplyToSelected(d => d + nudge, ripple);

        if (GUILayout.Button("Snap to Grid", GUILayout.Width(110)))
            ApplyToSelected(d => Snap(d, grid), ripple: false);

        if (GUILayout.Button("Distribute", GUILayout.Width(100)))
            DistributeSelected();

        if (GUILayout.Button("Normalize", GUILayout.Width(100)))
            NormalizeSelected();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }

    void DrawList()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", GUILayout.Width(30));
        GUILayout.Label("Select", GUILayout.Width(55));
        GUILayout.Label("Target", GUILayout.ExpandWidth(true));
        GUILayout.Label("baseDelay (s)", GUILayout.Width(120));
        GUILayout.Label("Action", GUILayout.Width(160));
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        var items = panel.items;
        for (int i = 0; i < items.Length; i++)
        {
            var it = items[i];
            if (it == null) continue;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(i.ToString(), GUILayout.Width(30));

            selection[i] = GUILayout.Toggle(selection[i], GUIContent.none, GUILayout.Width(55));

            var col = GUI.color;
            GUI.color = it.target ? Color.white : new Color(1, 0.8f, 0.8f);
            EditorGUI.BeginChangeCheck();
            it.target = (RectTransform)EditorGUILayout.ObjectField(it.target, typeof(RectTransform), true);
            if (EditorGUI.EndChangeCheck())
                MarkDirty();
            GUI.color = col;

            EditorGUI.BeginChangeCheck();
            it.delay = EditorGUILayout.FloatField(it.delay, GUILayout.Width(120));
            if (EditorGUI.EndChangeCheck())
                MarkDirty();

            it.action = (UIAnimAction)EditorGUILayout.ObjectField(it.action, typeof(UIAnimAction), false, GUILayout.Width(160));

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawFooter()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All", GUILayout.Width(100)))
            SetAllSelection(true);
        if (GUILayout.Button("Clear Selection", GUILayout.Width(120)))
            SetAllSelection(false);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Play", GUILayout.Width(80)))
            panel.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
        if (GUILayout.Button("Reset", GUILayout.Width(80)))
            panel.SendMessage("ResetToBaseImmediate", SendMessageOptions.DontRequireReceiver);

        EditorGUILayout.EndHorizontal();
    }

    void SetAllSelection(bool val)
    {
        for (int i = 0; i < selection.Length; i++) selection[i] = val;
        Repaint();
    }

    void ApplyToSelected(Func<float, float> op, bool ripple)
    {
        var items = panel.items;
        if (items == null || items.Length == 0) return;

        // Selected indices in ascending order
        var idx = Enumerable.Range(0, items.Length).Where(i => selection[i]).ToList();
        if (idx.Count == 0) return;

        Undo.RecordObject(panel, "Sequencer Edit");

        if (!ripple)
        {
            foreach (var i in idx)
                items[i].delay = op(items[i].delay);
        }
        else
        {
            // Ripple: shift everything from the earliest selected item forwards
            int pivot = idx.Min();
            float delta = op(items[pivot].delay) - items[pivot].delay;

            for (int i = pivot; i < items.Length; i++)
                items[i].delay += delta;
        }

        MarkDirty();
    }

    void DistributeSelected()
    {
        var items = panel.items;
        var idx = Enumerable.Range(0, items.Length).Where(i => selection[i]).ToArray();
        if (idx.Length < 2) return;

        Undo.RecordObject(panel, "Distribute Delays");

        // sort by current delay to maintain temporal order
        var sorted = idx.OrderBy(i => items[i].delay).ToArray();
        float start = items[sorted.First()].delay;
        float end   = items[sorted.Last()].delay;
        if (Mathf.Approximately(start, end))
        {
            // if same delay, use window's grid as spacing
            for (int k = 0; k < sorted.Length; k++)
                items[sorted[k]].delay = start + k * grid;
        }
        else
        {
            for (int k = 0; k < sorted.Length; k++)
                items[sorted[k]].delay = Mathf.Lerp(start, end, k / (float)(sorted.Length - 1));
        }

        MarkDirty();
    }

    void NormalizeSelected()
    {
        var items = panel.items;
        var idx = Enumerable.Range(0, items.Length).Where(i => selection[i]).ToArray();
        if (idx.Length == 0) return;

        Undo.RecordObject(panel, "Normalize Delays");

        // Keep selection order by index, start at 0, step by panel.stepDelay
        Array.Sort(idx);
        for (int k = 0; k < idx.Length; k++)
            items[idx[k]].delay = k * panel.defaultDelay;

        MarkDirty();
    }

    static float Snap(float v, float g)
    {
        if (g <= 0f) return v;
        return Mathf.Round(v / g) * g;
    }

    void MarkDirty()
    {
        EditorUtility.SetDirty(panel);
        Repaint();
    }
}

}
#endif