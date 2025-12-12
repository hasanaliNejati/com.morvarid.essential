namespace MorvaridEssential
{
    using UnityEngine;
    using DG.Tweening;

    [CreateAssetMenu(fileName = "JellyTiltButtonAnim", menuName = "UI/Button Anim/JellyTilt")]
    public class JellyTiltButtonAnimProfile : ButtonAnimProfileBase
    {
        public float downScaleMultiplier = 0.9f;
        public float downDuration = 0.1f;

        [Header("Tilt")]
        public float tiltAngleDeg = 6f;
        public Vector3 tiltAxis = new Vector3(0, 0, 1);
        public bool randomizeTiltSign = true;
        public float tiltDownDuration = 0.08f;
        public float tiltReturnDuration = 0.25f;
        public Ease tiltEaseDown = Ease.OutQuad;
        public Ease tiltEaseUp = Ease.OutQuad;

        public float upDuration = 0.15f;

        public override void OnDown(Transform target, Vector3 originalScale, Quaternion originalRotation)
        {
            target.DOKill();
            target.DOScale(originalScale * downScaleMultiplier, downDuration).SetEase(Ease.OutQuad);

            float sign = randomizeTiltSign ? (Random.value < 0.5f ? -1f : 1f) : 1f;
            Vector3 euler = tiltAxis.normalized * (tiltAngleDeg * sign);

            target.DOLocalRotateQuaternion(originalRotation * Quaternion.Euler(euler), tiltDownDuration)
                .SetEase(tiltEaseDown);
        }

        public override void OnUp(Transform target, Vector3 originalScale, Quaternion originalRotation)
        {
            target.DOKill();
            target.DOScale(originalScale, upDuration).SetEase(tiltEaseUp);

            target.DOLocalRotateQuaternion(originalRotation, tiltReturnDuration)
                .SetEase(tiltEaseUp);
        }
    }

}