// UIAnim_AxialOrbitalIn.cs

using UnityEngine;
using DG.Tweening;

namespace MorvaridEssential
{
    [CreateAssetMenu(menuName = "UI Anim/Axial Orbital In")]
    public class UIAnim_AxialOrbitalIn : UIAnimAction
    {
        [Header("Start Offset (from basePos)")]
        public float startOffsetX = -240f;

        public float startOffsetY = +120f;


        public float xDuration = 0.3f;

        public float yDuration = 0.3f;

        //
        // [Header("Timing (fractions of duration)")]
        // [Range(0f, 1f)] public float xDelayPortion = 0.00f;
        // [Range(0f, 1f)] public float yDelayPortion = 0.05f;
        // [Range(0.1f, 1f)] public float xDurPortion = 0.80f;
        // [Range(0.1f, 1f)] public float yDurPortion = 1.00f;
        //
        [Header("Easing per Axis")] public Ease xEase = Ease.OutElastic;
        public Ease yEase = Ease.OutElastic;

        [Header("Elastic (optional)")] public bool xElastic = false;
        public float xElasticAmplitude = 1.0f;
        public float xElasticPeriod = 0.35f;
        public bool yElastic = true;
        public float yElasticAmplitude = 1.0f;

        public float yElasticPeriod = 0.35f;
        //
        // [Header("Tilt (Z Rotation)")]
         public float startTiltDeg = -8f;
         public float tiltDurPortion = 0.60f;
        public Ease tiltEase = Ease.OutElastic;
        [SerializeField] public float rotationElasticAmplitude = 1.0f;
        [SerializeField] public float rotationElasticPeriod = 0.35f;
        //
        // [Header("Scale In")]
        // [Tooltip("شروع از 0 و رشد به baseScale")]
        public Ease scaleEase = Ease.OutElastic;
        public float scaleDuration = 1f;
        [SerializeField] public float scaleElasticAmplitude = 1.0f;
        [SerializeField] public float scaleElasticPeriod = 0.35f;

        public override Sequence Build(RectTransform target, Vector2 basePos, Vector3 baseScale, float baseRotZ,
            float delay)
        {
            var seq = DOTween.Sequence().SetAutoKill(false);
            seq.AppendInterval(delay);


            //
            // // حالت اولیه: آفست و اسکیل صفر
            Vector2 startPos = basePos + new Vector2(startOffsetX, startOffsetY);
             target.anchoredPosition = startPos;
            target.localScale = Vector3.zero;
             target.localEulerAngles = new Vector3(target.localEulerAngles.x, target.localEulerAngles.y, baseRotZ + startTiltDeg);
            //
            // CanvasGroup cg = null;
            // if (alsoFade)
            // {
            //     cg = GetOrAddCG(target);
            //     cg.alpha = fromAlpha;
            // }
            //
            // // زمان‌بندی
            // float xDelay = delay + duration * Mathf.Clamp01(xDelayPortion);
            // float yDelay = delay + duration * Mathf.Clamp01(yDelayPortion);
            // float xDur   = Mathf.Max(0.0001f, duration * Mathf.Clamp01(xDurPortion));
            // float yDur   = Mathf.Max(0.0001f, duration * Mathf.Clamp01(yDurPortion));
            // float tTilt  = Mathf.Max(0.0001f, duration * Mathf.Clamp01(tiltDurPortion));
            //
            // // --- حرکت X ---
            var tx = target.DOAnchorPosX(basePos.x, xDuration);
            if (xElastic) tx.SetEase(Ease.OutElastic, xElasticAmplitude, xElasticPeriod);
            else tx.SetEase(xEase);
            
            // // --- حرکت Y ---
            var ty = target.DOAnchorPosY(basePos.y, xDuration);
            if (yElastic) ty.SetEase(Ease.OutElastic, yElasticAmplitude, yElasticPeriod);
            else ty.SetEase(yEase);
            
            seq.Append(tx);
            seq.Join(ty);
            
            // --- Scale از صفر به baseScale ---
            seq.Join(target.DOScale(baseScale, scaleDuration)
                            .SetEase(scaleEase, scaleElasticAmplitude,scaleElasticPeriod));
            //
            // // --- Fade ---
            // if (alsoFade && cg != null)
            // {
            //     seq.Join(cg.DOFade(1f, duration).SetEase(ease).SetDelay(delay));
            // }
            //
            seq.Join(target.DOLocalRotate(new Vector3(target.localEulerAngles.x, target.localEulerAngles.y, baseRotZ),
                    tiltDurPortion)
                .SetEase(tiltEase,rotationElasticAmplitude,rotationElasticPeriod)
                );
            // // --- Tilt بازگشت ---

            return seq;
        }
    }
}