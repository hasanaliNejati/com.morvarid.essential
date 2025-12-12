// UIAnim_StretchSlideIn.cs
// نیازمند DOTween و کلاس پایه UIAnimAction شما

using UnityEngine;
using DG.Tweening;

namespace MorvaridEssential
{
    [CreateAssetMenu(menuName = "UI Anim/Stretch Slide In")]
    public class UIAnim_StretchSlideIn : UIAnimAction
    {
        public enum Direction { Left, Right, Up, Down }

        [Header("Slide-In")]
        public Direction from = Direction.Left;
        public float distance = 300f;             // فاصله شروع خارج از صحنه
        [Range(0.2f, 0.95f)] public float travelPortion = 0.65f; // چند درصد از duration صرف حرکت شود
        public Ease travelEase = Ease.OutCubic;

        [Header("Stretch (Squash & Stretch)")]
        [Tooltip("ضریب کش آمدن در راستای حرکت")]
        public float stretchAlong = 1.25f;
        [Tooltip("ضریب فشردگی در عمود بر حرکت")]
        public float squashPerp = 0.85f;
        [Range(0.1f, 0.9f)] public float stretchPortionOfTravel = 0.6f; // چه بخشی از زمان حرکت صرف رسیدن به حداکثر استرچ شود
        public Ease stretchEase = Ease.OutCubic;

        [Header("Settle (Overshoot کوچک پس از رسیدن)")]
        public float settleOvershoot = 1.05f; // 1.0 = بدون اورشوت
        [Range(0.05f, 0.6f)] public float settleTime = 0.12f;
        public Ease settleEaseOut = Ease.OutQuad;
        public float settleReturnTime = 0.08f;
        public Ease settleEaseBack = Ease.InQuad;

        public override Sequence Build(RectTransform target, Vector2 basePos, Vector3 baseScale, float baseRotZ, float delay)
        {
            var seq = DOTween.Sequence();
            seq.SetAutoKill(false);

            // 1) حالت اولیه: پوزیشن آفست + آلفا (در صورت نیاز)
            Vector2 startPos = basePos + OffsetFromDirection(from, distance);
            target.anchoredPosition = startPos;
            target.localScale = baseScale; // از اسکایل پایه شروع می‌کنیم

            CanvasGroup cg = null;
            if (alsoFade)
            {
                cg = GetOrAddCG(target);
                cg.alpha = fromAlpha;
            }

            // 2) زمان‌بندی بخش‌ها
            float tTravel = Mathf.Max(0.0001f, duration * travelPortion);
            float tStretch = tTravel * Mathf.Clamp01(stretchPortionOfTravel);

            // 3) Scale هدف برای مرحله‌ی stretch (وابسته به جهت)
            Vector3 stretchVec = StretchVectorForDirection(from, stretchAlong, squashPerp);
            Vector3 stretchScale = new Vector3(
                baseScale.x * stretchVec.x,
                baseScale.y * stretchVec.y,
                baseScale.z
            );

            // 4) توالی
            seq.AppendInterval(delay);

            // 4a) حرکت به موقعیت پایه
            seq.Join(target.DOAnchorPos(basePos, tTravel).SetEase(travelEase));

            // 4b) همزمان: رسیدن به حداکثر Stretch در بخشی از مسیر
            // سپس به تدریج به نزدیکِ baseScale برگردیم (اگر بخواهیم)
            if (tStretch > 0.0001f)
            {
                // به استرچ برس
                seq.Join(target.DOScale(stretchScale, tStretch).SetEase(stretchEase));

                // باقی‌مانده‌ی زمان حرکت را کمی به سمت baseScale برگردیم تا ورود، طبیعی‌تر شود
                float tRelax = Mathf.Max(0f, tTravel - tStretch);
                if (tRelax > 0.0001f)
                {
                    seq.Join(target.DOScale(baseScale, tRelax).SetEase(Ease.OutQuad));
                }
            }
            else
            {
                // اگر tStretch عملاً صفر شد، فقط به baseScale برگرد
                seq.Join(target.DOScale(baseScale, tTravel).SetEase(Ease.OutQuad));
            }

            // 4c) Fade همزمان با حرکت (اگر فعال است)
            if (alsoFade && cg != null)
                seq.Join(cg.DOFade(1f, tTravel).SetEase(ease));

            // 5) Settle کوچک پس از رسیدن
            if (settleOvershoot > 1.0001f && settleTime > 0f)
            {
                Vector3 settleUp = baseScale * settleOvershoot;
                seq.Append(target.DOScale(settleUp, settleTime).SetEase(settleEaseOut));
                if (settleReturnTime > 0f)
                    seq.Append(target.DOScale(baseScale, settleReturnTime).SetEase(settleEaseBack));
            }

            return seq;
        }

        private static Vector2 OffsetFromDirection(Direction d, float dist)
        {
            return d switch
            {
                Direction.Left  => new Vector2(-Mathf.Abs(dist), 0f),
                Direction.Right => new Vector2(+Mathf.Abs(dist), 0f),
                Direction.Up    => new Vector2(0f, +Mathf.Abs(dist)),
                Direction.Down  => new Vector2(0f, -Mathf.Abs(dist)),
                _ => Vector2.zero
            };
        }

        // در راستای حرکت کشیده، در عمود بر آن فشرده
        private static Vector2 AxisFactors(Direction d, float along, float perp)
        {
            return d switch
            {
                Direction.Left  => new Vector2(along, perp),  // حرکت افقی → X کشیده، Y فشرده
                Direction.Right => new Vector2(along, perp),
                Direction.Up    => new Vector2(perp, along),  // حرکت عمودی → Y کشیده، X فشرده
                Direction.Down  => new Vector2(perp, along),
                _ => Vector2.one
            };
        }

        private static Vector3 StretchVectorForDirection(Direction d, float along, float perp)
        {
            var f = AxisFactors(d, along, perp);
            return new Vector3(f.x, f.y, 1f);
        }
    }
}
