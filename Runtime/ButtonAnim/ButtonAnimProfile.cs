namespace MorvaridEssential
{
    using UnityEngine;
    using DG.Tweening;

    public abstract class ButtonAnimProfileBase : ScriptableObject
    {
        public abstract void OnDown(Transform target, Vector3 originalScale, Quaternion originalRotation);
        public abstract void OnUp(Transform target, Vector3 originalScale, Quaternion originalRotation);
    }

}