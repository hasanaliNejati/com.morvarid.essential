#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MorvaridEssential; // UIAnimAction & MenuItemsAnimatorSODriven

public class SimpleMenuItemsTimelineWindow : EditorWindow
{
    public static SimpleMenuItemsTimelineWindow Instance;

    // ==== Data Source ====
    private Animalo animator;
    private bool selectAnimation = false;

    // ==== Sidebar (foldouts) ====
    [SerializeField] private float sidebarWidth = 260f;
    private const float MinSidebar = 160f, MaxSidebar = 520f, SplitterWidth = 4f;
    private bool _resizingSplitter;
    private Vector2 sidebarScroll;
    private readonly List<bool> _fold = new List<bool>();

    // ==== Bottom Drag Bar (no labels) ====
    [SerializeField] private UIAnimAction dropAction; // فیلد بدون لیبل
    [SerializeField] private float dropDelayUnit = 0.1f; // فیلد بدون لیبل
    [SerializeField] private float tailPaddingPx = 240f; // پدینگ انتهای تایم‌لاین (px)

    // ==== Zoom/Scroll ====
    private float pixelsPerSecond = 100f;
    private Vector2 scroll;

    // ==== Ruler ====
    private const float HeaderHeight = 64f;
    private const float RulerHeight = 32f;

    // ==== Track ====
    private const float MinTrackHeight = 160f;
    private float currentTrackHeight = MinTrackHeight;

    // ==== Visual ====
    [SerializeField] private float gridYSpacing = 24f;
    [SerializeField] private float mainLineWidth = 10f;
    [SerializeField] private float targetMajorTickPx = 96f;

    // ==== Padding (no negative time) ====
    [SerializeField] private float leftPaddingPx = 48f; // فاصله بصری سمت چپ
    private float LeftPadPx => Mathf.Max(0f, leftPaddingPx);

    // ==== Selection / dragging ====
    private int hotIndex = -1;
    private bool isDragging = false;
    private readonly HashSet<int> selected = new HashSet<int>();
    private bool isMarquee = false;
    private Vector2 marqueeStart;
    private Rect marqueeRect;

    // Group drag
    private int anchorIndex = -1;
    private float anchorOffsetTime = 0f;
    private readonly List<int> dragIndices = new List<int>();
    private readonly List<float> dragStartTimes = new List<float>();
    private float anchorStartTime = 0f;

    // Snap (قدیمی)
    private bool snapEnabled = true;
    private float snapStep = 0.1f;

    // ==== Snap+Selection Enhancements (جدید) ====
    [SerializeField] private bool snapToMarkers = true;
    [SerializeField] private bool snapToGrid = true;
    [SerializeField] private bool snapAutoFromRuler = true; // از minorStep خط‌کش
    [SerializeField] private float snapThresholdPx = 8f; // شعاع مغناطیسی (px)
    [SerializeField] private bool showSnapGuide = true;
    private float? snapGuideTime = null; // خط راهنمای اسنپ

    // ==== Scale selection ====
    private enum ScaleHandle
    {
        None,
        Left,
        Right
    }

    private bool isScaling = false;
    private ScaleHandle activeHandle = ScaleHandle.None;
    private float selMinTime = 0f, selMaxTime = 0f;
    private readonly List<int> scaleIndices = new List<int>();
    private readonly List<float> scaleStartTimes = new List<float>();
    private const float HandleHalfSize = 6f;
    private Rect cachedLeftHandleRect, cachedRightHandleRect, cachedBandRect;

    // عرض نمای تایم‌لاین برای کلَمپ دقیق اسکرول
    private float lastViewWidth;

    // ==== Max timeline (120s) ====
    private const float MaxTimelineSeconds = 120f;
    private float ClampTime(float t) => Mathf.Clamp(t, 0f, MaxTimelineSeconds);

    private void NotifyIfOverMax(float t)
    {
        if (t > MaxTimelineSeconds)
            ShowNotification(new GUIContent("⏱️ Mxs time is 120"));
    }

    // ==== Nice steps ====
    private static readonly double[] NiceTimeSteps =
    {
        0.001, 0.002, 0.005,
        0.01, 0.02, 0.05,
        0.1, 0.2, 0.5,
        1, 2, 5,
        10, 15, 30,
        60, 120, 300,
        600, 900, 1800,
        3600, 7200, 10800
    };

    [MenuItem("Tools/Simple Timeline (Menu Items)")]
    public static void ShowWindow()
    {
        var win = GetWindow<SimpleMenuItemsTimelineWindow>("Menu Items Timeline");
        win.minSize = new Vector2(720, 340);
        win.Show();
        Instance = win;
    }

    public void SetAnimator(Animalo menuItems)
    {
        animator = menuItems;
        selectAnimation = true;
    }

    private void OnGUI()
    {
        DrawHeader();

        if (animator == null)
        {
            EditorGUILayout.HelpBox("یک MenuItemsAnimatorSODriven انتخاب کن.", MessageType.Info);
            return;
        }

        EnsureFoldouts();

        using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
        {
            // Sidebar
            GUILayout.BeginVertical(GUILayout.Width(sidebarWidth));
            DrawSidebar();
            GUILayout.EndVertical();

            // Splitter
            var splitterRect = GUILayoutUtility.GetRect(SplitterWidth, 1, GUILayout.Width(SplitterWidth),
                GUILayout.ExpandHeight(true));
            DrawSplitter(splitterRect);

            // Timeline area (right)
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawTimelineArea();
            GUILayout.EndVertical();
        }

        Repaint();
    }

    private void EnsureFoldouts()
    {
        if (animator?.items == null)
        {
            _fold.Clear();
            return;
        }

        int n = animator.items.Length;
        while (_fold.Count < n) _fold.Add(true); // پیش‌فرض: باز
        if (_fold.Count > n) _fold.RemoveRange(n, _fold.Count - n);
    }

    // ====================== HEADER ======================
    private void DrawHeader()
    {
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        using (new GUILayout.HorizontalScope())
        {
            // Animator بدون لیبل با عرض ثابت
            animator = (Animalo)EditorGUILayout.ObjectField(
                animator, typeof(Animalo), true, GUILayout.Width(260));

            GUILayout.Space(16);

            // Zoom
            GUILayout.Label("Zoom", GUILayout.Width(40));
            float minSec = 0.001f, maxSec = 1f;
            float logMin = Mathf.Log10(minSec), logMax = Mathf.Log10(maxSec);
            float secPerPixel = 1f / Mathf.Max(0.00001f, pixelsPerSecond);
            float logCur = Mathf.Log10(secPerPixel);
            float newLog = GUILayout.HorizontalSlider(logCur, logMin, logMax, GUILayout.Width(200));
            pixelsPerSecond = 1f / Mathf.Pow(10f, newLog);

            GUILayout.FlexibleSpace();

            // Snap + وابسته‌ها
            snapEnabled = GUILayout.Toggle(snapEnabled, "Snap", "Button", GUILayout.Width(60));
            if (snapEnabled)
            {
                snapToMarkers = GUILayout.Toggle(snapToMarkers, "Markers", "Button", GUILayout.Width(80));
                snapToGrid = GUILayout.Toggle(snapToGrid, "Grid", "Button", GUILayout.Width(60));
                snapAutoFromRuler = GUILayout.Toggle(snapAutoFromRuler, "Auto", "Button", GUILayout.Width(60));
                showSnapGuide = GUILayout.Toggle(showSnapGuide, "Guide", "Button", GUILayout.Width(70));
            }
        }
    }

    // ====================== SIDEBAR (FOLDOUTS + DROP BAR) ======================
    private void DrawSidebar()
    {
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            sidebarScroll = EditorGUILayout.BeginScrollView(sidebarScroll);
            if (animator.items != null)
            {
                for (int i = 0; i < animator.items.Length; i++)
                {
                    var it = animator.items[i] ??= new Animalo.Item();

                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            string title = it.target ? it.target.name : (it.action ? it.action.name : $"Item {i}");
                            _fold[i] = EditorGUILayout.Foldout(_fold[i], title, true);

                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(22)))
                            {
                                MoveItem(i, i - 1);
                                break;
                            }

                            if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(22)))
                            {
                                MoveItem(i, i + 1);
                                break;
                            }

                            if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(22)))
                            {
                                RemoveItem(i);
                                break;
                            }
                        }

                        // Delay (با کَلمپ و هشدار)
                        EditorGUI.BeginChangeCheck();
                        float newDelay = EditorGUILayout.FloatField("Delay (s)", it.delay);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(animator, "Edit Delay");
                            float raw = (snapEnabled && snapStep > 0f)
                                ? Mathf.Round(newDelay / snapStep) * snapStep
                                : newDelay;

                            if (raw > MaxTimelineSeconds) NotifyIfOverMax(raw);
                            it.delay = ClampTime(raw);
                            animator.items[i] = it;
                            EditorUtility.SetDirty(animator);
                        }

                        // محتوا
                        if (_fold[i])
                        {
                            EditorGUI.indentLevel++;
                            var newTarget =
                                (RectTransform)EditorGUILayout.ObjectField("Target", it.target, typeof(RectTransform),
                                    true);
                            if (newTarget != it.target)
                            {
                                Undo.RecordObject(animator, "Edit Target");
                                it.target = newTarget;
                                animator.items[i] = it;
                                EditorUtility.SetDirty(animator);
                            }

                            var newAction = (UIAnimAction)EditorGUILayout.ObjectField("Action", it.action,
                                typeof(UIAnimAction), false);
                            if (newAction != it.action)
                            {
                                Undo.RecordObject(animator, "Edit Action");
                                it.action = newAction;
                                animator.items[i] = it;
                                EditorUtility.SetDirty(animator);
                            }

                            EditorGUI.indentLevel--;
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            // نوار دراپ پایین
            DrawBottomDropBar();
        }
    }

    private void DrawBottomDropBar()
    {
        Rect r = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
        Color bg = EditorGUIUtility.isProSkin ? new Color(0.13f, 0.13f, 0.13f) : new Color(0.85f, 0.85f, 0.85f);
        EditorGUI.DrawRect(r, bg);

        // [UIAnimAction] [float]
        float pad = 6f;
        float fieldHeight = r.height - 8f;
        float actionW = Mathf.Max(140f, (r.width - pad * 3) * 0.7f);
        Rect actionRect = new Rect(r.x + pad, r.y + 4f, actionW, fieldHeight);
        Rect floatRect = new Rect(actionRect.xMax + pad, r.y + 4f, r.width - actionW - pad * 3, fieldHeight);

        dropAction =
            (UIAnimAction)EditorGUI.ObjectField(actionRect, GUIContent.none, dropAction, typeof(UIAnimAction), false);
        dropDelayUnit = EditorGUI.FloatField(floatRect, GUIContent.none, dropDelayUnit);

        HandleDrop(r);
    }

    private void HandleDrop(Rect r)
    {
        var e = Event.current;
        if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
        if (!r.Contains(e.mousePosition)) return;

        bool canAccept = HasAcceptableReference(DragAndDrop.objectReferences);
        DragAndDrop.visualMode = canAccept ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

        if (e.type == EventType.DragPerform && canAccept)
        {
            DragAndDrop.AcceptDrag();
            CreateItemsFromDragged(DragAndDrop.objectReferences);
        }

        e.Use();
    }

    private bool HasAcceptableReference(UnityEngine.Object[] refsObj)
    {
        if (animator == null || animator.transform == null) return false;
        foreach (var obj in refsObj)
            if (TryGetRectTransform(obj, out var rt) && rt.transform.IsChildOf(animator.transform))
                return true;
        return false;
    }

    private void CreateItemsFromDragged(UnityEngine.Object[] refsObj)
    {
        if (animator == null) return;

        Undo.RecordObject(animator, "Add Items (Drag & Drop)");
        var list = new List<Animalo.Item>(animator.items ??
                                                            Array.Empty<Animalo.Item>());
        int startCount = list.Count;
        int created = 0;

        foreach (var obj in refsObj)
        {
            if (!TryGetRectTransform(obj, out var rt)) continue;
            if (!rt.transform.IsChildOf(animator.transform)) continue;

            var item = new Animalo.Item
            {
                target = rt,
                action = dropAction,
                delay = ClampTime(Mathf.Max(0f, dropDelayUnit * (startCount + created)))
            };
            if (Mathf.Approximately(item.delay, MaxTimelineSeconds) && dropDelayUnit > 0f)
                NotifyIfOverMax(item.delay);

            list.Add(item);
            created++;
        }

        animator.items = list.ToArray();
        EditorUtility.SetDirty(animator);
        EnsureFoldouts();
    }

    // ایجاد با زمان پایهٔ مشخص (DnD روی کل پنل)
    private void CreateItemsFromDraggedAtTime(UnityEngine.Object[] refsObj, float baseTime)
    {
        if (animator == null) return;

        Undo.RecordObject(animator, "Add Items (Drag & Drop @Time)");
        var list = new List<Animalo.Item>(animator.items ??
                                                            Array.Empty<Animalo.Item>());
        int created = 0;

        float baseClamped = ClampTime(baseTime);
        foreach (var obj in refsObj)
        {
            if (!TryGetRectTransform(obj, out var rt)) continue;
            if (!rt.transform.IsChildOf(animator.transform)) continue;

            var item = new Animalo.Item
            {
                target = rt,
                action = dropAction,
                delay = ClampTime(Mathf.Max(0f, baseClamped + dropDelayUnit * created))
            };
            if (item.delay >= MaxTimelineSeconds && dropDelayUnit > 0f)
                NotifyIfOverMax(item.delay);

            list.Add(item);
            created++;
        }

        animator.items = list.ToArray();
        EditorUtility.SetDirty(animator);
        EnsureFoldouts();
    }

    private bool TryGetRectTransform(UnityEngine.Object obj, out RectTransform rt)
    {
        rt = null;
        if (obj is RectTransform rtt)
        {
            rt = rtt;
            return true;
        }

        if (obj is GameObject go)
        {
            rt = go.GetComponent<RectTransform>();
            return rt != null;
        }

        if (obj is Component c)
        {
            rt = c.GetComponent<RectTransform>();
            return rt != null;
        }

        return false;
    }

    private void MoveItem(int from, int to)
    {
        if (animator?.items == null) return;
        if (to < 0 || to >= animator.items.Length) return;
        Undo.RecordObject(animator, "Reorder Item");
        var arr = animator.items;
        (arr[to], arr[from]) = (arr[from], arr[to]);
        animator.items = arr;
        EditorUtility.SetDirty(animator);

        bool f = _fold[from];
        _fold.RemoveAt(from);
        _fold.Insert(to, f);

        if (selected.Remove(from)) selected.Add(to);
    }

    private void RemoveItem(int index)
    {
        if (animator?.items == null || index < 0 || index >= animator.items.Length) return;
        Undo.RecordObject(animator, "Remove Item");
        var list = new List<Animalo.Item>(animator.items);
        list.RemoveAt(index);
        animator.items = list.ToArray();
        EditorUtility.SetDirty(animator);
        if (index < _fold.Count) _fold.RemoveAt(index);
        selected.Remove(index);
    }

    private void DrawSplitter(Rect r)
    {
        EditorGUIUtility.AddCursorRect(r, MouseCursor.ResizeHorizontal);
        var e = Event.current;
        if (e.type == EventType.MouseDown && r.Contains(e.mousePosition))
        {
            _resizingSplitter = true;
            e.Use();
        }

        if (_resizingSplitter && e.type == EventType.MouseDrag)
        {
            sidebarWidth = Mathf.Clamp(sidebarWidth + e.delta.x, MinSidebar, MaxSidebar);
            e.Use();
        }

        if (e.type == EventType.MouseUp) _resizingSplitter = false;

        EditorGUI.DrawRect(new Rect(r.x, r.y, 1, position.height), new Color(0, 0, 0, 0.3f));
    }

    // ====================== TIMELINE (right) ======================
    private void DrawTimelineArea()
    {
        // قبل از هرچیز، مطمئن شو آیتمی خارج از بازه نیست
        ClampAllItemsIfNeeded();

        // 1) عرض واقعی نمای تایم‌لاین
        Rect scrollRect = GUILayoutUtility.GetRect(0, position.height - HeaderHeight, GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));
        currentTrackHeight = Mathf.Max(1f, scrollRect.height - RulerHeight);
        lastViewWidth = scrollRect.width;

        // 2) پهنای محتوا
        float totalWidth = GetTotalContentWidth(scrollRect.width);
        Rect contentRect = new Rect(0, 0, totalWidth, RulerHeight + currentTrackHeight);

        scroll = GUI.BeginScrollView(scrollRect, scroll, contentRect, false, false, GUI.skin.horizontalScrollbar,
            GUIStyle.none);
        scroll.y = 0f;

        EditorGUI.DrawRect(new Rect(0, 0, contentRect.width, contentRect.height),
            EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.95f, 0.95f, 0.95f));

        DrawTimeRuler(new Rect(0, 0, contentRect.width, RulerHeight));
        if (selected.Count >= 2) DrawSelectionBandAndHandles(contentRect);
        DrawTrackArea(new Rect(0, RulerHeight, contentRect.width, currentTrackHeight));

        HandleTimelineDragAndDrop(contentRect);
        HandleInput(contentRect);

        if (isMarquee)
        {
            Handles.BeginGUI();
            Color marqueeFill =
                EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.10f) : new Color(0f, 0f, 0f, 0.10f);
            Handles.DrawSolidRectangleWithOutline(marqueeRect, marqueeFill, Color.clear);
            Handles.EndGUI();
        }

        // Tooltip زمان زیر موس (کَلمپ‌شده)
        var e = Event.current;
        float hoveredTime = ClampTime(XToTime(e.mousePosition.x + scroll.x));
        var tipRect = new Rect(e.mousePosition.x + 12, RulerHeight + 6, 160, 18);
        GUI.Label(tipRect, FormatTimeForTooltip(hoveredTime), EditorStyles.miniLabel);

        // خط راهنمای اسنپ
        if (showSnapGuide && snapGuideTime.HasValue)
        {
            float xGuide = TimeToX(snapGuideTime.Value);
            Handles.BeginGUI();
            Handles.color = EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.8f, 1f, 0.85f)
                : new Color(0f, 0.45f, 0.95f, 0.9f);
            Handles.DrawLine(new Vector3(xGuide, 0), new Vector3(xGuide, RulerHeight + currentTrackHeight));
            Handles.EndGUI();
        }

        GUI.EndScrollView();
    }

    // همه آیتم‌ها را داخل بازه ۰..۱۲۰ نگه می‌داریم
    private void ClampAllItemsIfNeeded()
    {
        if (animator?.items == null) return;
        bool changed = false;
        for (int i = 0; i < animator.items.Length; i++)
        {
            var it = animator.items[i];
            if (it == null) continue;
            float clamped = ClampTime(it.delay);
            if (!Mathf.Approximately(clamped, it.delay))
            {
                NotifyIfOverMax(it.delay);
                it.delay = clamped;
                animator.items[i] = it;
                changed = true;
            }
        }

        if (changed) EditorUtility.SetDirty(animator);
    }

    // Zoom-to-Fit (میانبر: F)
    private void ZoomToFit(float rightPadPx = 240f)
    {
        float viewWidth = position.width - sidebarWidth - SplitterWidth;
        float usablePx = Mathf.Max(1f, viewWidth - LeftPadPx - rightPadPx);

        float lastTime = 0f;
        if (animator?.items != null)
            foreach (var it in animator.items)
                if (it != null && it.delay > lastTime)
                    lastTime = it.delay;

        lastTime = Mathf.Min(lastTime, MaxTimelineSeconds);
        if (lastTime <= 0f) return;

        pixelsPerSecond = usablePx / lastTime;
        scroll.x = 0f;
    }

    // همیشه حداقل tailPaddingPx در انتها نگه می‌داریم
    private float GetTotalContentWidth(float viewWidthPx)
    {
        float lastTime = 0f;
        if (animator?.items != null && animator.items.Length > 0)
        {
            for (int i = 0; i < animator.items.Length; i++)
            {
                var it = animator.items[i];
                if (it == null) continue;
                if (it.delay > lastTime) lastTime = it.delay;
            }
        }

        lastTime = Mathf.Min(lastTime, MaxTimelineSeconds); // سقف 120s

        float baseWidth = LeftPadPx + lastTime * pixelsPerSecond;
        float contentWidth = Mathf.Max(baseWidth + tailPaddingPx, viewWidthPx);
        return contentWidth;
    }

    // ---- اورلود سازگار برای جاهایی که ورودی نداریم ----
    private float GetTotalContentWidth()
    {
        float vw = (lastViewWidth > 1f) ? lastViewWidth : Mathf.Max(1f, position.width - sidebarWidth - SplitterWidth);
        return GetTotalContentWidth(vw);
    }

    private void GetTickSteps(float pxPerSec, out double majorStep, out double minorStep)
    {
        double bestStep = 1, bestDiff = double.MaxValue;
        foreach (var s in NiceTimeSteps)
        {
            double px = s * pxPerSec;
            double diff = Math.Abs(px - targetMajorTickPx);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestStep = s;
            }
        }

        majorStep = bestStep;

        double[] divs = { 5, 4, 2 };
        double chosenMinor = majorStep / 5.0;
        foreach (var d in divs)
        {
            double cand = majorStep / d;
            if (cand * pxPerSec >= 10.0)
            {
                chosenMinor = cand;
                break;
            }
        }

        if (chosenMinor * pxPerSec < 10.0) chosenMinor = majorStep / 2.0;
        minorStep = chosenMinor;
    }

    private string FormatTimeLabel(double t, double majorStep)
    {
        if (majorStep < 0.01) return $"{t:0.000}s";
        if (majorStep < 0.1) return $"{t:0.00}s";
        if (majorStep < 1.0) return $"{t:0.0}s";

        int sign = t < 0 ? -1 : 1;
        double at = Math.Abs(t);
        int totalSec = (int)Math.Round(at);
        int sec = totalSec % 60;
        int min = (totalSec / 60) % 60;
        int hour = (totalSec / 3600);
        string core = hour > 0 ? $"{hour}:{min:00}:{sec:00}" :
            at >= 60.0 ? $"{min}:{sec:00}" : $"{sec}s";
        return sign < 0 ? $"-{core}" : core;
    }

    private string FormatTimeForTooltip(float t)
    {
        double abs = Math.Abs(t);
        return abs >= 1.0 ? $"{(t < 0 ? "-" : "")}{abs:0.###}s" : $"{(t < 0 ? "-" : "")}{abs:0.000}s";
    }

    private void DrawTimeRuler(Rect r)
    {
        EditorGUI.DrawRect(r,
            EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.88f, 0.88f, 0.88f));
        GetTickSteps(pixelsPerSecond, out double major, out double minor);

        Handles.BeginGUI();
        float y0 = r.yMin;
        double tMin = XToTime(r.xMin + scroll.x);
        double tMax = XToTime(r.xMax + scroll.x);

        // Minor
        Handles.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.06f) : new Color(0, 0, 0, 0.06f);
        double startMinor = Math.Floor(tMin / minor) * minor;
        for (double t = startMinor; t <= tMax + 1e-6; t += minor)
        {
            float x = TimeToX((float)t);
            if (x < r.xMin - 1 || x > r.xMax + 1) continue;
            Handles.DrawLine(new Vector3(x, y0 + r.height * 0.35f), new Vector3(x, y0 + r.height));
        }

        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 10,
            normal =
            {
                textColor = EditorGUIUtility.isProSkin ? new Color(0.92f, 0.92f, 0.92f) : new Color(0.15f, 0.15f, 0.15f)
            }
        };

        // Major
        Handles.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.12f) : new Color(0, 0, 0, 0.12f);
        double startMajor = Math.Floor(tMin / major) * major;
        for (double t = startMajor; t <= tMax + 1e-6; t += major)
        {
            float x = TimeToX((float)t);
            if (x < r.xMin - 1 || x > r.xMax + 1) continue;
            Handles.DrawLine(new Vector3(x, y0), new Vector3(x, y0 + r.height));
            var rect = new Rect(x - 36, y0 + 2, 72, 16);
            GUI.Label(rect, FormatTimeLabel(t, major), labelStyle);
        }

        // خط صفر
        float xZero = TimeToX(0f);
        Handles.color = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.7f, 1f, 0.9f) : new Color(0f, 0.4f, 0.9f, 0.9f);
        Handles.DrawLine(new Vector3(xZero, y0), new Vector3(xZero, y0 + r.height));
        Handles.EndGUI();
    }

    private void DrawTrackArea(Rect r)
    {
        float cy = r.yMin + r.height * 0.5f;
        DrawVerticalGrid(r);

        // خط صفر
        {
            Handles.BeginGUI();
            var prev = Handles.color;
            Handles.color = EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.7f, 1f, 0.6f)
                : new Color(0f, 0.4f, 0.9f, 0.6f);
            float xZero = TimeToX(0f);
            Handles.DrawLine(new Vector3(xZero, r.yMin), new Vector3(xZero, r.yMax));
            Handles.color = prev;
            Handles.EndGUI();
        }

        // خطوط افقی
        {
            Handles.BeginGUI();
            var prev = Handles.color;
            Color minorCol = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.06f) : new Color(0f, 0f, 0f, 0.08f);
            Handles.color = minorCol;

            int halfCount = Mathf.Max(1, Mathf.FloorToInt((r.height * 0.5f) / Mathf.Max(4f, gridYSpacing)));
            for (int i = 1; i <= halfCount; i++)
            {
                float yUp = cy - i * gridYSpacing;
                float yDown = cy + i * gridYSpacing;
                if (yUp >= r.yMin) Handles.DrawLine(new Vector3(r.xMin, yUp), new Vector3(r.xMax, yUp));
                if (yDown <= r.yMax) Handles.DrawLine(new Vector3(r.xMin, yDown), new Vector3(r.xMax, yDown));
            }

            // خط مرکزی ضخیم
            Color mainCol = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.25f) : new Color(0f, 0f, 0f, 0.28f);
            Handles.color = mainCol;
            Handles.DrawAAPolyLine(mainLineWidth, new Vector3(r.xMin, cy, 0f), new Vector3(r.xMax, cy, 0f));

            Handles.color = prev; // ریست رنگ
            Handles.EndGUI();
        }

        // مارکرها
        if (animator?.items != null)
        {
            for (int i = 0; i < animator.items.Length; i++)
                DrawMarker(i, cy);
        }

        // کادر انتخاب (Marquee)
        if (isMarquee)
        {
            var fill = new Color(0.20f, 0.40f, 1f, 0.25f); // آبی نیمه‌شفاف
            EditorGUI.DrawRect(marqueeRect, fill);

            var outline = new Color(0.20f, 0.60f, 1f, 1f);
            Handles.BeginGUI();
            var prev = Handles.color;
            Handles.color = outline;

            Handles.DrawAAPolyLine(2f,
                new Vector3(marqueeRect.xMin, marqueeRect.yMin),
                new Vector3(marqueeRect.xMax, marqueeRect.yMin),
                new Vector3(marqueeRect.xMax, marqueeRect.yMax),
                new Vector3(marqueeRect.xMin, marqueeRect.yMax),
                new Vector3(marqueeRect.xMin, marqueeRect.yMin)
            );

            Handles.color = prev;
            Handles.EndGUI();
        }
    }

    private void DrawVerticalGrid(Rect r)
    {
        GetTickSteps(pixelsPerSecond, out double major, out double minor);
        Handles.BeginGUI();
        double tMin = XToTime(r.xMin + scroll.x);
        double tMax = XToTime(r.xMax + scroll.x);

        Handles.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.04f) : new Color(0, 0, 0, 0.05f);
        double startMinor = Math.Floor(tMin / minor) * minor;
        for (double t = startMinor; t <= tMax + 1e-6; t += minor)
        {
            float x = TimeToX((float)t);
            Handles.DrawLine(new Vector3(x, r.yMin), new Vector3(x, r.yMax));
        }

        Handles.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.07f) : new Color(0, 0, 0, 0.08f);
        double startMajor = Math.Floor(tMin / major) * major;
        for (double t = startMajor; t <= tMax + 1e-6; t += major)
        {
            float x = TimeToX((float)t);
            Handles.DrawLine(new Vector3(x, r.yMin), new Vector3(x, r.yMax));
        }

        Handles.EndGUI();
    }

    private void DrawMarker(int index, float centerY)
    {
        var items = animator.items;
        if (items == null || index < 0 || index >= items.Length) return;
        var it = items[index];
        if (it == null) return;

        float t = it.delay;
        float x = TimeToX(t);

        float size = 14f;
        var p = new[]
        {
            new Vector3(x, centerY - size * 0.5f, 0f),
            new Vector3(x + size * 0.5f, centerY, 0f),
            new Vector3(x, centerY + size * 0.5f, 0f),
            new Vector3(x - size * 0.5f, centerY, 0f),
        };

        bool isSel = selected.Contains(index);

        Color fill = isSel ? new Color(1, 1, 1, 1f) : new Color(1, 1, 1, 0.9f);
        Color outline = isSel ? new Color(0.2f, 0.6f, 1f, 1f) : new Color(0, 0, 0, 0.6f);

        Handles.BeginGUI();
        Handles.color = fill;
        Handles.DrawAAConvexPolygon(p);
        Handles.color = outline;
        Handles.DrawPolyLine(new Vector3[] { p[0], p[1], p[2], p[3], p[0] });
        Handles.EndGUI();

        if (isSel)
        {
            string label =
                (it.target != null ? it.target.name : (it.action != null ? it.action.name : $"Item {index}")) +
                $"  ({t:0.###}s)";
            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.LowerCenter };
            var labelRect = new Rect(x - 60, centerY - 28, 120, 18);
            GUI.Label(labelRect, label, style);
        }

        Rect hit = new Rect(x - size * 0.7f, centerY - size * 0.7f, size * 1.4f, size * 1.4f);
        EditorGUIUtility.AddCursorRect(hit, MouseCursor.Pan);
    }

    // ====================== Input / selection ======================
    private void HandleInput(Rect contentRect)
    {
        var e = Event.current;
        bool ctrl = (e.control || e.command);
        Vector2 mouseContent = e.mousePosition;

        // === Zoom (Ctrl/Alt + Wheel) ===
        if (e.type == EventType.ScrollWheel && (e.control || e.modifiers == EventModifiers.Alt))
        {
            float mouseTime = XToTime(mouseContent.x + scroll.x);
            float secPerPixel = 1f / Mathf.Max(0.000001f, pixelsPerSecond);
            float zoomFactor = Mathf.Pow(1.2f, e.delta.y);

            const float MIN_SEC_PER_PIXEL = 0.001f;
            const float MAX_SEC_PER_PIXEL = 1f;

            secPerPixel = Mathf.Clamp(secPerPixel * zoomFactor, MIN_SEC_PER_PIXEL, MAX_SEC_PER_PIXEL);
            pixelsPerSecond = 1f / secPerPixel;

            float newX = TimeToX(mouseTime);
            float dx = newX - e.mousePosition.x - scroll.x;

            scroll.x = Mathf.Clamp(scroll.x + dx, 0f, GetMaxScroll());
            e.Use();
            return;
        }

        // === Zoom-to-Fit (F) ===
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F)
        {
            ZoomToFit(tailPaddingPx);
            e.Use();
        }

        // === Scale handles ===
        if (e.type == EventType.MouseDown && e.button == 0 && selected.Count >= 2)
        {
            if (cachedLeftHandleRect.Contains(mouseContent))
            {
                BeginScale(ScaleHandle.Left);
                e.Use();
                return;
            }

            if (cachedRightHandleRect.Contains(mouseContent))
            {
                BeginScale(ScaleHandle.Right);
                e.Use();
                return;
            }
        }
        else if (e.type == EventType.MouseDrag && isScaling)
        {
            DoScale(mouseContent, !e.alt);
            e.Use();
            return;
        }
        else if (e.type == EventType.MouseUp && isScaling)
        {
            EndScale();
            e.Use();
            return;
        }

        // === Marker select/drag ===
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            hotIndex = HitTestMarker(mouseContent, contentRect);
            if (hotIndex >= 0)
            {
                if (ctrl)
                {
                    if (selected.Contains(hotIndex)) selected.Remove(hotIndex);
                    else selected.Add(hotIndex);
                }
                else
                {
                    if (!selected.Contains(hotIndex))
                    {
                        selected.Clear();
                        selected.Add(hotIndex);
                    }
                }

                BeginGroupDrag(mouseContent);
                if (selected.Count == 1)
                {
                    int i = selected.First();
                    if (i >= 0 && i < _fold.Count) _fold[i] = true;
                }

                e.Use();
            }
            else
            {
                Rect trackRect = new Rect(0, RulerHeight, contentRect.width, currentTrackHeight);
                if (trackRect.Contains(mouseContent))
                {
                    isMarquee = true;
                    marqueeStart = mouseContent;
                    marqueeRect = new Rect(marqueeStart, Vector2.zero);
                    e.Use();
                }
            }
        }
        else if (e.type == EventType.MouseDrag && e.button == 0)
        {
            if (isDragging)
            {
                DragGroup(mouseContent, !e.alt);
                e.Use();
            }
            else if (isMarquee)
            {
                marqueeRect = MakeRectFromPoints(marqueeStart, mouseContent);
                marqueeRect = Rect.MinMaxRect(
                    marqueeRect.xMin,
                    Mathf.Max(marqueeRect.yMin, RulerHeight),
                    marqueeRect.xMax,
                    Mathf.Min(marqueeRect.yMax, RulerHeight + currentTrackHeight)
                );
                e.Use();
            }
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            if (isDragging)
            {
                EndGroupDrag();
                e.Use();
            }
            else if (isMarquee)
            {
                ApplyMarqueeSelection(ctrl, contentRect);
                isMarquee = false;
                e.Use();
            }
        }

        // === Pan (Middle mouse or Alt+LMB drag) ===
        if ((e.button == 2 || (e.button == 0 && e.alt)) && e.type == EventType.MouseDrag)
        {
            scroll.x = Mathf.Clamp(scroll.x - e.delta.x, 0f, GetMaxScroll());
            e.Use();
        }

        // === Delete ===
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete && selected.Count > 0)
        {
            if (animator.items != null)
            {
                Undo.RecordObject(animator, "Delete Items");
                var list = new List<Animalo.Item>(animator.items);
                foreach (var idx in selected.OrderByDescending(i => i))
                {
                    if (idx >= 0 && idx < list.Count)
                    {
                        list.RemoveAt(idx);
                        if (idx < _fold.Count) _fold.RemoveAt(idx);
                    }
                }

                animator.items = list.ToArray();
                EditorUtility.SetDirty(animator);
                selected.Clear();
                hotIndex = -1;
            }

            e.Use();
        }

        // === Select All / Deselect All ===
        if (e.type == EventType.KeyDown && ((e.control || e.command) && e.keyCode == KeyCode.A && !e.shift))
        {
            selected.Clear();
            for (int i = 0; i < (animator.items?.Length ?? 0); i++) selected.Add(i);
            e.Use();
        }

        if (e.type == EventType.KeyDown && ((e.control || e.command) && e.shift && e.keyCode == KeyCode.A))
        {
            selected.Clear();
            hotIndex = -1;
            e.Use();
        }
    }

    // ==== Scaling ====
    private void BeginScale(ScaleHandle handle)
    {
        if (animator?.items == null || selected.Count < 2) return;
        isScaling = true;
        activeHandle = handle;

        scaleIndices.Clear();
        scaleStartTimes.Clear();
        selMinTime = float.PositiveInfinity;
        selMaxTime = float.NegativeInfinity;

        foreach (var idx in selected.OrderBy(i => i))
        {
            scaleIndices.Add(idx);
            var it = animator.items[idx];
            if (it == null) continue;
            float t = it.delay;
            scaleStartTimes.Add(t);
            if (t < selMinTime) selMinTime = t;
            if (t > selMaxTime) selMaxTime = t;
        }

        if (Mathf.Approximately(selMaxTime, selMinTime)) selMaxTime = selMinTime + 0.0001f;
        Undo.RecordObject(animator, "Scale Items");
    }

    private void DoScale(Vector2 mouseContent, bool allowSnap)
    {
        if (animator?.items == null) return;

        float mouseTime = XToTime(mouseContent.x + scroll.x);
        if (allowSnap) mouseTime = FindNearestSnapTime(mouseTime, selected);

        float pivot, edge0, edgeNew;
        if (activeHandle == ScaleHandle.Right)
        {
            pivot = selMinTime;
            edge0 = selMaxTime;
            edgeNew = mouseTime;
        }
        else
        {
            pivot = selMaxTime;
            edge0 = selMinTime;
            edgeNew = mouseTime;
        }

        float denom = (edge0 - pivot);
        if (Mathf.Approximately(denom, 0f)) denom = (denom >= 0f ? 1e-6f : -1e-6f);
        float factor = (edgeNew - pivot) / denom;

        for (int i = 0; i < scaleIndices.Count; i++)
        {
            int idx = scaleIndices[i];
            var it = animator.items[idx];
            if (it == null) continue;

            float t0 = scaleStartTimes[i];
            float t = pivot + (t0 - pivot) * factor;
            it.delay = ClampTime(t);
            animator.items[idx] = it;
        }

        EditorUtility.SetDirty(animator);

        snapGuideTime = allowSnap ? ClampTime(edgeNew) : (float?)null;
    }

    private void EndScale()
    {
        isScaling = false;
        activeHandle = ScaleHandle.None;
        scaleIndices.Clear();
        scaleStartTimes.Clear();
        snapGuideTime = null;
    }

    // ==== Group drag ====
    private void BeginGroupDrag(Vector2 mouseContent)
    {
        if (animator?.items == null) return;
        isDragging = true;

        anchorIndex = hotIndex;
        var anchorIt = animator.items[anchorIndex];
        anchorStartTime = anchorIt?.delay ?? 0f;
        anchorOffsetTime = XToTime(mouseContent.x + scroll.x) - anchorStartTime;

        dragIndices.Clear();
        dragStartTimes.Clear();
        foreach (var idx in selected.OrderBy(i => i))
        {
            dragIndices.Add(idx);
            var it = animator.items[idx];
            float t = it?.delay ?? 0f;
            dragStartTimes.Add(t);
        }
    }

    private void DragGroup(Vector2 mouseContent, bool allowSnap)
    {
        if (animator?.items == null) return;

        float tAnchorRaw = XToTime(mouseContent.x + scroll.x) - anchorOffsetTime;
        float tAnchor = Mathf.Max(0f, tAnchorRaw);

        float snappedAnchor = allowSnap ? FindNearestSnapTime(tAnchor, selected) : tAnchor;
        float delta = snappedAnchor - anchorStartTime;

        Undo.RecordObject(animator, "Move Items");
        for (int i = 0; i < dragIndices.Count; i++)
        {
            int idx = dragIndices[i];
            var it = animator.items[idx];
            if (it == null) continue;

            float t0 = dragStartTimes[i];
            it.delay = ClampTime(t0 + delta);
            animator.items[idx] = it;
        }

        EditorUtility.SetDirty(animator);

        snapGuideTime = allowSnap ? ClampTime(snappedAnchor) : (float?)null;
    }

    private void EndGroupDrag()
    {
        isDragging = false;
        anchorIndex = -1;
        dragIndices.Clear();
        dragStartTimes.Clear();
        snapGuideTime = null;
    }

    // ==== Marquee select ====
    private void ApplyMarqueeSelection(bool additive, Rect contentRect)
    {
        if (animator?.items == null) return;

        float cy = RulerHeight + currentTrackHeight * 0.5f;
        var hitList = new List<int>();
        for (int i = 0; i < animator.items.Length; i++)
        {
            var it = animator.items[i];
            if (it == null) continue;
            float t = it.delay;
            float x = TimeToX(t);
            var center = new Vector2(x, cy);
            if (marqueeRect.Contains(center)) hitList.Add(i);
        }

        if (!additive) selected.Clear();
        foreach (var idx in hitList) selected.Add(idx);
    }

    private static Rect MakeRectFromPoints(Vector2 a, Vector2 b)
    {
        var min = Vector2.Min(a, b);
        var max = Vector2.Max(a, b);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private int HitTestMarker(Vector2 mouseContent, Rect contentRect)
    {
        if (animator?.items == null) return -1;

        float cy = RulerHeight + currentTrackHeight * 0.5f;
        float size = 14f;
        for (int i = 0; i < animator.items.Length; i++)
        {
            var it = animator.items[i];
            if (it == null) continue;
            float t = it.delay;
            float x = TimeToX(t);
            Rect hit = new Rect(x - size * 0.8f, cy - size * 0.8f, size * 1.6f, size * 1.6f);
            if (hit.Contains(mouseContent)) return i;
        }

        return -1;
    }

    // ==== Selection band + handles (multi) ====
    private void DrawSelectionBandAndHandles(Rect contentRect)
    {
        if (selected.Count < 2 || animator?.items == null) return;

        float minT = float.PositiveInfinity, maxT = float.NegativeInfinity;
        foreach (var idx in selected)
        {
            if (idx < 0 || idx >= animator.items.Length) continue;
            var it = animator.items[idx];
            if (it == null) continue;
            float t = it.delay;
            if (t < minT) minT = t;
            if (t > maxT) maxT = t;
        }

        if (!float.IsFinite(minT) || !float.IsFinite(maxT)) return;

        float xMin = TimeToX(minT);
        float xMax = TimeToX(maxT);
        float bandX = Mathf.Min(xMin, xMax);
        float bandW = Mathf.Max(1f, Mathf.Abs(xMax - xMin));

        float trackY = RulerHeight;
        float trackH = currentTrackHeight;

        Rect band = new Rect(bandX - 1f, trackY, bandW + 2f, trackH);
        Color subtle = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0.18f);
        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(band, subtle, Color.clear);
        Handles.EndGUI();

        Rect leftH = new Rect(band.xMin - HandleHalfSize, trackY, HandleHalfSize * 2f, trackH);
        Rect rightH = new Rect(band.xMax - HandleHalfSize, trackY, HandleHalfSize * 2f, trackH);

        DrawHandleGrip(leftH);
        DrawHandleGrip(rightH);

        EditorGUIUtility.AddCursorRect(leftH, MouseCursor.ResizeHorizontal);
        EditorGUIUtility.AddCursorRect(rightH, MouseCursor.ResizeHorizontal);

        cachedLeftHandleRect = leftH;
        cachedRightHandleRect = rightH;
        cachedBandRect = band;
    }

    private void DrawHandleGrip(Rect r)
    {
        Handles.BeginGUI();
        Handles.color = new Color(0.2f, 0.5f, 1f, 0.9f);
        float midX = r.center.x;
        Handles.DrawLine(new Vector3(midX - 2, r.yMin + 4), new Vector3(midX - 2, r.yMax - 4));
        Handles.DrawLine(new Vector3(midX + 2, r.yMin + 4), new Vector3(midX + 2, r.yMax - 4));
        Handles.EndGUI();
    }

    // ==== Time <-> X ====
    private float TimeToX(float t) => LeftPadPx + ClampTime(t) * pixelsPerSecond;
    private float XToTime(float x) => (x - LeftPadPx) / pixelsPerSecond;

    private float GetMaxScroll() =>
        Mathf.Max(0f, GetTotalContentWidth(lastViewWidth) - lastViewWidth);

    // ==== Snap helper ====
    private float FindNearestSnapTime(float tRaw, HashSet<int> ignoreIndices = null)
    {
        if (!snapEnabled) return ClampTime(tRaw);

        float bestT = tRaw;
        float bestDxPx = float.MaxValue;
        float xRaw = TimeToX(tRaw);

        // 1) مارکرها
        if (snapToMarkers && animator?.items != null)
        {
            for (int i = 0; i < animator.items.Length; i++)
            {
                if (ignoreIndices != null && ignoreIndices.Contains(i)) continue;
                var it = animator.items[i];
                if (it == null) continue;

                float xCand = TimeToX(it.delay);
                float dx = Mathf.Abs(xCand - xRaw);
                if (dx < bestDxPx && dx <= snapThresholdPx)
                {
                    bestDxPx = dx;
                    bestT = it.delay;
                }
            }
        }

        // 2) گرید
        if (snapToGrid)
        {
            GetTickSteps(pixelsPerSecond, out double major, out double minor);
            double step = snapAutoFromRuler ? minor : Math.Max(0.000001, snapStep);
            double k = Math.Round(tRaw / step);
            float tGrid = (float)(k * step);

            float dx = Mathf.Abs(TimeToX(tGrid) - xRaw);
            if (dx < bestDxPx && dx <= snapThresholdPx)
            {
                bestDxPx = dx;
                bestT = tGrid;
            }
        }

        // 3) fallback دستی
        if (snapEnabled && !snapAutoFromRuler)
        {
            float tManual = Mathf.Round(tRaw / snapStep) * snapStep;
            float dx = Mathf.Abs(TimeToX(tManual) - xRaw);
            if (dx < bestDxPx)
            {
                bestDxPx = dx;
                bestT = tManual;
            }
        }

        return ClampTime(bestT);
    }

    // ==== DnD روی کل پنل ====
    private void HandleTimelineDragAndDrop(Rect contentRect)
    {
        var e = Event.current;
        if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;

        Rect full = new Rect(0, 0, contentRect.width, contentRect.height);
        if (!full.Contains(e.mousePosition)) return;

        bool canAccept = HasAcceptableReference(DragAndDrop.objectReferences);
        DragAndDrop.visualMode = canAccept ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

        float tRaw = XToTime(e.mousePosition.x + scroll.x);
        float t = (!e.alt) ? FindNearestSnapTime(tRaw, null) : Mathf.Max(0f, tRaw);
        t = ClampTime(t);

        if (canAccept && showSnapGuide) snapGuideTime = t;

        if (e.type == EventType.DragPerform && canAccept)
        {
            DragAndDrop.AcceptDrag();
            CreateItemsFromDraggedAtTime(DragAndDrop.objectReferences, t);
            snapGuideTime = null;
        }

        e.Use();
    }
}
#endif