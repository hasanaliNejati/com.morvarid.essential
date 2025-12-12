using UnityEngine.Serialization;

namespace MorvaridEssential
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using DG.Tweening;
    using Sirenix.OdinInspector; // برای ShowIf

    public class ButtonScaleTween : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private ButtonAnimProfileBase customProfile; // اختیاری

        [Header("Original Transform")] [SerializeField]
        private bool setStaticOriginalTransform = true;

        [ShowIf(nameof(setStaticOriginalTransform))] [SerializeField]
        private Vector3 originalScale = Vector3.one;

        [ShowIf(nameof(setStaticOriginalTransform))] [SerializeField]
        private Quaternion originalRotation = Quaternion.identity;

        void Awake()
        {
            if (!setStaticOriginalTransform)
            {
                originalScale = transform.localScale;
                originalRotation = transform.localRotation;
            }
        }

        void OnDisable()
        {
            transform.DOKill();
            transform.localScale = originalScale;
            transform.localRotation = originalRotation;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (customProfile != null)
            {
                customProfile.OnDown(transform, originalScale, originalRotation);
            }
            else
            {
                // Soft پیش‌فرض
                transform.DOKill();
                transform.DOScale(originalScale * 0.9f, 0.1f).SetEase(Ease.OutQuad);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (customProfile != null)
            {
                customProfile.OnUp(transform, originalScale, originalRotation);
            }
            else
            {
                // Soft پیش‌فرض
                transform.DOKill();
                transform.DOScale(originalScale, 0.45f).SetEase(Ease.OutElastic,1.5f,0.35f);
            }
        }
    }
}