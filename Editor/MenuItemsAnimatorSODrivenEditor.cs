#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MorvaridEssential.Editor
{
    [CustomEditor(typeof(Animalo))]
    public class MenuItemsAnimatorSODrivenEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Open Animator Panel"))
            {
                var myTarget = (Animalo)target;
                SimpleMenuItemsTimelineWindow.ShowWindow();
                SimpleMenuItemsTimelineWindow.Instance.SetAnimator(myTarget);
            }
        }
    }
}

#endif