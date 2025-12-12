using System;
using DG.Tweening;
using UnityEngine;

namespace MorvaridEssential.Transation
{
    public class Transition : MonoBehaviour
    {
        [SerializeField] private GameObject clouds;
        [SerializeField] private GameObject blocker;
        
        [SerializeField] private Vector3 offset =  new Vector3(2500,0,0);
        [SerializeField] private float duration = 1;

        private void Start()
        {
            clouds.transform.position = transform.position;
            var s = DOTween.Sequence();
            s.AppendCallback(() => { clouds.gameObject.SetActive(true); });
            s.Append(clouds.transform.DOMove(transform.position - offset, duration / 2).SetEase(Ease.Linear));
        }

        public void ShowClud(Action done)
        {
            clouds.transform.position = transform.position + offset;
            var s = DOTween.Sequence();
            s.AppendCallback(() =>
            {
                clouds.gameObject.SetActive(true);
                blocker.gameObject.SetActive(true);
            });

            s.Append(clouds.transform.DOMove(transform.position, duration / 2).SetEase(Ease.Linear));

            s.AppendCallback(() =>
            {
                blocker.gameObject.SetActive(false);
                done.Invoke();
                
            });
            s.Append(clouds.transform.DOMove(transform.position - offset, duration / 2).SetEase(Ease.Linear));
        }
    }
}