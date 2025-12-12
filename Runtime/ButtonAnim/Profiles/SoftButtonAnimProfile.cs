namespace MorvaridEssential
{
    using UnityEngine;
    using DG.Tweening;

    [CreateAssetMenu(fileName = "SoftButtonAnim", menuName = "UI/Button Anim/Soft")]
    public class SoftButtonAnimProfile : ButtonAnimProfileBase
    {
        public float downScaleMultiplier = 0.9f;
        public float downDuration = 0.1f;
        public float upDuration = 0.15f;
        public Ease upEase = Ease.OutQuad;

        public override void OnDown(Transform target, Vector3 originalScale, Quaternion originalRotation)
        {
            target.DOKill();
            target.DOScale(originalScale * downScaleMultiplier, downDuration).SetEase(Ease.OutQuad);
        }

        public override void OnUp(Transform target, Vector3 originalScale, Quaternion originalRotation)
        {
            target.DOKill();
            target.DOScale(originalScale, upDuration).SetEase(upEase);
        }
    }

}