using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MorvaridEssential
{
    // MenuItemsAnimatorSODriven.cs
    [DisallowMultipleComponent]
    public class Animalo : MonoBehaviour
    {
        [Serializable]
        public class Item
        {
            public RectTransform target;
            public float delay = 0.1f;
            public float outDelay = 0.1f;
            public UIAnimAction action;
            public UIAnimAction outAction;
        }

        [Header("Run Settings")] public bool playOnEnable = true;
        public bool resetBeforePlay = true;
        public float defaultDelay = 0.1f;
        public float baseDelay = 0f;

        [Header("Items")] public Item[] items;

        // کش حالت پایه
        readonly Dictionary<RectTransform, Vector2> _basePos = new();
        readonly Dictionary<RectTransform, Vector3> _baseScale = new();
        readonly Dictionary<RectTransform, float> _baseRotZ = new();

        readonly List<Tween> _showing = new();
        readonly List<Tween> _hiding = new();

        void Awake()
        {
            CacheBases();
            PanelScript panel;
            if (TryGetComponent<PanelScript>(out panel))
            {
                panel.onDisableEvent += Hide;
            }
        }

        void OnEnable()
        {
            if (playOnEnable) Show();
        }

        void OnDisable() => KillHiding();

        void CacheBases()
        {
            _basePos.Clear();
            _baseScale.Clear();
            _baseRotZ.Clear();
            if (items == null) return;

            foreach (var it in items)
            {
                if (it?.target == null) continue;
                var t = it.target;
                if (_basePos.ContainsKey(t)) continue;

                _basePos[t] = t.anchoredPosition;
                _baseScale[t] = t.localScale;
                _baseRotZ[t] = t.localEulerAngles.z;
            }
        }

        public void Show()
        {
            if (items == null) return;

            if (resetBeforePlay)
                ResetToBaseImmediate();

            KillHiding();
            KillShowing();
            // CacheBases();

            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                if (it == null || it.target == null || it.action == null) continue;

                var t = it.target;
                var pos = _basePos[t];
                var sca = _baseScale[t];
                var rot = _baseRotZ[t];

                float delay = it.delay + baseDelay;
                var seq = it.action.Build(t, pos, sca, rot, delay);
                if (seq != null)
                {
                    seq.SetAutoKill(false);
                    _showing.Add(seq);
                }
            }
        }

        public void Hide()
        {
            if (items == null) return;

            KillHiding();

            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                if (it == null || it.target == null) continue;
                var t = it.target;

                float delay = it.outDelay;

                // اگر outAction داشت
                if (it.outAction != null)
                {
                    // Base رو از کش بخون
                    var pos = _basePos[t];
                    var sca = _baseScale[t];
                    var rot = _baseRotZ[t];

                    var seq = it.outAction.Build(t, pos, sca, rot, delay);
                    if (seq != null) _hiding.Add(seq);
                }
                else
                {
                    // از همون seq قبلی برگشت بزن
                    if (i < _showing.Count && _showing[i] != null)
                    {
                        var seq = _showing[i];
                        _hiding.Add(DOTween.Sequence()
                            .AppendInterval(delay) // تاخیر به ثانیه
                            .AppendCallback(() =>
                            {
                                seq.Goto(seq.Duration(false), true);
                                seq.PlayBackwards();
                            }));
                    }
                }
            }
        }

        public void ResetToBaseImmediate()
        {
            if (items == null) return;
            foreach (var it in items)
            {
                if (it?.target == null) continue;
                var t = it.target;

                if (_basePos.TryGetValue(t, out var p)) t.anchoredPosition = p;
                if (_baseScale.TryGetValue(t, out var s)) t.localScale = s;
                if (_baseRotZ.TryGetValue(t, out var z))
                    t.localEulerAngles = new Vector3(t.localEulerAngles.x, t.localEulerAngles.y, z);

                var cg = t.GetComponent<CanvasGroup>();
                if (cg) cg.alpha = 1f;
            }
        }

        void KillShowing()
        {
            foreach (var tw in _showing)
                if (tw != null && tw.IsActive())
                {
                    tw.Kill();
                }

            _showing.Clear();
        }

        void KillHiding()
        {
            foreach (var tw in _hiding)
                if (tw != null && tw.IsActive())
                {
                    tw.Kill();
                }

            _hiding.Clear();
        }

        [Button("Play Now")]
        void CtxPlay() => Show();

        [Button("Hide Now")]
        void CtxHide() => Hide();

        [Button("Reset Immediate")]
        void CtxReset() => ResetToBaseImmediate();
    }
}