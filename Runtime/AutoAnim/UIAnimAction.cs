using DG.Tweening;
using UnityEngine;

namespace MorvaridEssential
{
    // UIAnimAction.cs
    public abstract class UIAnimAction : ScriptableObject
    {
        [Header("Common")]
        public float duration = 0.45f;
        public Ease ease = Ease.OutCubic;
        public bool alsoFade = false;
        [Range(0f,1f)] public float fromAlpha = 0f;

        // آماده‌سازی حالت شروع (پوزیشن/اسکیل/چرخش/آلفا…)
        // و ساخت تویین نهایی. برگرداندن Sequence برای انعطاف بیشتر.
        public abstract Sequence Build(RectTransform target,
            Vector2 basePos,
            Vector3 baseScale,
            float baseRotZ,
            float delay);

        // ابزار مشترک برای آلفا
        protected CanvasGroup GetOrAddCG(RectTransform rt)
        {
            var cg = rt.GetComponent<CanvasGroup>();
            if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }
    }

}