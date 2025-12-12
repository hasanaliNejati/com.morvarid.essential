namespace MorvaridEssential
{
    using UnityEngine;
    using DG.Tweening;

    [CreateAssetMenu(fileName = "ElasticButtonAnim", menuName = "UI/Button Anim/Elastic")]
    public class ElasticButtonAnimProfile : ButtonAnimProfileBase
    {
        public float downScaleMultiplier = 0.9f;
        public float downDuration = 0.1f;
        public float upDuration = 0.4f;
        public float amplitude = 1f;
        public float period = 0.35f;

        public override void OnDown(Transform target, Vector3 originalScale, Quaternion originalRotation)
        {
            target.DOKill();
            target.DOScale(originalScale * downScaleMultiplier, downDuration).SetEase(Ease.OutQuad);
        }

        public override void OnUp(Transform target, Vector3 originalScale, Quaternion originalRotation)
        {
            target.DOKill();
            target.DOScale(originalScale, upDuration).SetEase(Ease.OutElastic, amplitude, period);
        }
    }

}