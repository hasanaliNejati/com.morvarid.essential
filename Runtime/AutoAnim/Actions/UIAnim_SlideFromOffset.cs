
namespace MorvaridEssential
{
    // UIAnim_SlideFromBottom.cs
    using UnityEngine;
    using DG.Tweening;

    [CreateAssetMenu(menuName = "UI Anim/Slide From Offset")]
    public class UIAnim_SlideFromOffset : UIAnimAction
    {
        [Header("Slide Params")] public float offsetX;
        public float offsetY;

        public float overshoot = 1.4f;

        public override Sequence Build(RectTransform target, Vector2 basePos, Vector3 baseScale, float baseRotZ,
            float delay)
        {
            target.anchoredPosition = basePos + new Vector2(offsetX, offsetY);

            CanvasGroup cg = null;
            if (alsoFade)
            {
                cg = GetOrAddCG(target);
                cg.alpha = fromAlpha;
            }

            var seq = DOTween.Sequence().SetDelay(delay);
            seq.Append(target.DOAnchorPos(basePos, duration).SetEase(ease, overshoot));

            if (alsoFade && cg != null)
                seq.Join(cg.DOFade(1f, duration).SetEase(ease));

            return seq;
        }
    }
}