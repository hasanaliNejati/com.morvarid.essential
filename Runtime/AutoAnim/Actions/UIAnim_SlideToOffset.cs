
namespace MorvaridEssential
{
    using UnityEngine;
    using DG.Tweening;

    [CreateAssetMenu(menuName = "UI Anim/Slide To Offset")]
    public class UIAnim_SlideToOffset : UIAnimAction
    {
        [Header("Slide Params")] public float offsetX;
        public float offsetY;

        public float overshoot = 1.4f;

        public override Sequence Build(RectTransform target, Vector2 basePos, Vector3 baseScale, float baseRotZ,
            float delay)
        {
            var destination = target.anchoredPosition + new Vector2(offsetX, offsetY);

            CanvasGroup cg = null;
            if (alsoFade)
            {
                cg = GetOrAddCG(target);
                cg.alpha = fromAlpha;
            }

            var seq = DOTween.Sequence().SetDelay(delay);
            seq.Append(target.DOAnchorPos(destination, duration).SetEase(ease, overshoot));

            if (alsoFade && cg != null)
                seq.Join(cg.DOFade(1f, duration).SetEase(ease));
            return seq;
        }
    }
}