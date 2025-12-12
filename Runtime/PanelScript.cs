using System;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class PanelScript : MonoBehaviour
{
    
    public float exitTime = 1f;
    public bool disableImmediately;
    public bool unscaledTime;
    public string exitAnimationName = "end panel";
    public string replayAnimName = "replay";

    public bool startFad;
    public bool endFad;
    public bool replayAnim;

    [SerializeField] private Animator animator;

    public bool activeSelf
    {
        get => _active;
    }

    public Action onEnableEvent;
    public Action onDisableEvent;

    private bool _active;
    private bool _disablePending;

    private CanvasGroup _canvasGroup;
    private CanvasGroup canvasGroup => _canvasGroup ??= GetComponent<CanvasGroup>();

    private void Awake()
    {
        _active = gameObject.activeSelf;
        canvasGroup.alpha = _active ? 1f : 0f;
        canvasGroup.blocksRaycasts = _active;
        TryGetComponent<Animator>(out animator);
    }

    public void SetActive(bool active)
    {
        SetActive(active, false);
    }

    public void SetActive(bool active, bool immediate)
    {
        if (active == _active) return;

        canvasGroup.DOKill();
        _active = active;

        if (active)
        {
            if (_disablePending)
            {
                _disablePending = false;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            
            // Re-enable animator if it was disabled
            if (animator && !animator.enabled)
            {
                animator.enabled = true;
            }
            
            onEnableEvent?.Invoke();

            if (replayAnim && animator && !immediate)
            {
                animator.Play(replayAnimName, -1, 0f);
            }

            canvasGroup.blocksRaycasts = true;
            
            if (immediate)
            {
                canvasGroup.alpha = 1f;
            }
            else
            {
                canvasGroup.alpha = startFad ? 0f : 1f;
                if (startFad)
                {
                    canvasGroup.DOFade(1f, exitTime).SetUpdate(unscaledTime);
                }
            }
        }
        else
        {
            if (immediate || disableImmediately || exitTime <= 0)
            {
                // Stop all animations immediately
                if (animator)
                {
                    animator.enabled = false;
                }

                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 0f;
                _disablePending = false;
                onDisableEvent?.Invoke();
                gameObject.SetActive(false);
            }
            else
            {
                _disablePending = true;
                onDisableEvent?.Invoke();

                if (animator)
                {
                    animator.Play(exitAnimationName);
                }

                canvasGroup.blocksRaycasts = false;
                canvasGroup.DOFade(endFad ? 0f : 1f, exitTime)
                    .SetUpdate(unscaledTime)
                    .OnComplete(() =>
                    {
                        if (!_active)
                        {
                            _disablePending = false;
                            gameObject.SetActive(false);
                        }
                    });
            }
        }
    }
}