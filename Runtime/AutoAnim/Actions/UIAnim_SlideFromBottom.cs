
namespace MorvaridEssential
{
    // UIAnim_SlideFromBottom.cs
    using UnityEngine;
    using DG.Tweening;

    [CreateAssetMenu(menuName = "UI Anim/Slide From Bottom")]
    public class UIAnim_SlideFromBottom : UIAnimAction
    {
        [Header("Slide Params")]
        public float offsetY = 420f; // فاصله شروع

        public override Sequence Build(RectTransform target, Vector2 basePos, Vector3 baseScale, float baseRotZ, float delay)
        {
            // حالت شروع
            target.anchoredPosition = basePos + new Vector2(0f, -Mathf.Abs(offsetY));

            CanvasGroup cg = null;
            if (alsoFade)
            {
                cg = GetOrAddCG(target);
                cg.alpha = fromAlpha;
            }

            // تویین‌ها
            var seq = DOTween.Sequence().SetDelay(delay);
            seq.Append(target.DOAnchorPos(basePos, duration).SetEase(ease));

            if (alsoFade && cg != null)
                seq.Join(cg.DOFade(1f, duration).SetEase(ease));

            return seq;
        }
    }

}