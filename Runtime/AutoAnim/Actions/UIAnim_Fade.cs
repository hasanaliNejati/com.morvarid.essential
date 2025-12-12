// UIAnim_Fade.cs

using UnityEngine;
using DG.Tweening;

namespace MorvaridEssential
{
    [CreateAssetMenu(menuName = "UI Anim/Fade")]
    public class UIAnim_Fade : UIAnimAction
    {
        [Header("Fade Params")]
        [Range(0f,1f)] public float from = 0f;
        [Range(0f,1f)] public float to   = 1f;

        public override Sequence Build(RectTransform target, Vector2 basePos, Vector3 baseScale, float baseRotZ, float delay)
        {
            var seq = DOTween.Sequence().SetAutoKill(false);
            seq.AppendInterval(delay);

            // گرفتن CanvasGroup (یا اضافه کردن اگر نباشه)
            var cg = GetOrAddCG(target);

            // حالت اولیه
            cg.alpha = from;

            // تویین
            seq.Append(cg.DOFade(to, duration).SetEase(ease));

            return seq;
        }
    }
}