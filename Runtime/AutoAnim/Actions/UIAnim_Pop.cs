
namespace MorvaridEssential
{
// UIAnim_Pop.cs
    using UnityEngine;
    using DG.Tweening;

    [CreateAssetMenu(menuName = "UI Anim/Pop")]
    public class UIAnim_Pop : UIAnimAction
    {
        [Header("Scale Params")]
        public float fromScale = 0.6f;
        public float overshoot = 1.4f; // شدت Back

        public override Sequence Build(RectTransform target, Vector2 basePos, Vector3 baseScale, float baseRotZ, float delay)
        {
            // شروع
            target.localScale = baseScale * Mathf.Max(0.0001f, fromScale);

            CanvasGroup cg = null;
            if (alsoFade)
            {
                cg = GetOrAddCG(target);
                cg.alpha = fromAlpha;
            }
            
            //Debug.Log(delay);
            
            var seq = DOTween.Sequence();
            seq.AppendInterval(delay);
            seq.Append(target.DOScale(baseScale, duration).SetEase(Ease.OutBack, overshoot));

            if (alsoFade && cg != null)
                seq.Join(cg.DOFade(1f, duration).SetEase(ease));

            return seq;
        }
    }

}