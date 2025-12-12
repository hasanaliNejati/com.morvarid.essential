
namespace MorvaridEssential
{
    using UnityEngine;
    using DG.Tweening;

    [CreateAssetMenu(menuName = "UI Anim/PopOut")]
    public class UIAnim_Pop_out : UIAnimAction
    {
        [Header("Scale Params")]
        public float toScale = 00f;
        public float overshoot = 1.4f; // شدت Back

        public override Sequence Build(RectTransform target, Vector2 basePos, Vector3 baseScale, float baseRotZ, float delay)
        {

            CanvasGroup cg = null;
            if (alsoFade)
            {
                cg = GetOrAddCG(target);
                cg.alpha = fromAlpha;
            }
            
            //Debug.Log(delay);
            
            var seq = DOTween.Sequence();
            seq.AppendInterval(delay);
            seq.Append(target.DOScale(toScale, duration).SetEase(Ease.InBack, overshoot));

            if (alsoFade && cg != null)
                seq.Join(cg.DOFade(1f, duration).SetEase(ease));

            return seq;
        }
    }
}