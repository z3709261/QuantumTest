using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor for QTAnimator State Assets
/// </summary>
[CustomEditor(typeof(GameAnimatorStateAsset), true)]
public class GameAnimatorStateAssetEditor : Editor
{
    /// <summary>
    /// A dictionary of existing editors
    /// </summary>
    Dictionary<int, Editor> behaviourEditors = new Dictionary<int, Editor>();

    /// <summary>
    /// The current behaviour being focused on.
    /// </summary>
    int behaviourIndex = 0;

    // The current animation clip.
    AnimationClip clip;

    // The editor for the animator clip
    Editor animEditor = null;

    // If true, the animation preview is hidden.
    bool hideAnimationPreview;

    public bool disableAnimPreview;

    // Show for the animation header toggle
    readonly string showAnimationHeader = "Show Animation Preview";

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GameAnimatorStateAsset container = target as GameAnimatorStateAsset;

        if (container.stateBehaviours == null)
            container.stateBehaviours = new List<CustomAnimatorBehaviourAsset>();

        for (int i = 0; i < container.stateBehaviours.Count; i++)
        {
            if (container.stateBehaviours[i] == null)
            {
                continue;
            }
        }

        for (int i = 0; i < container.stateBehaviours.Count; i++)
        {
            if (container.stateBehaviours[i] == null)
                continue;

            Editor editor;
            if (!behaviourEditors.TryGetValue(container.stateBehaviours[i].GetInstanceID(), out editor))
            {
                editor = CreateEditor(container.stateBehaviours[i]);
                behaviourEditors.Add(container.stateBehaviours[i].GetInstanceID(), editor);
            }

            editor?.DrawHeader();
            editor?.OnInspectorGUI();
        }

        if (container.animatorState != null)
        {
            clip = container.animatorState.motion as AnimationClip;
        }
        EditorGUILayout.Space();
        if (!disableAnimPreview)
        {
            hideAnimationPreview = EditorGUILayout.Foldout(hideAnimationPreview, showAnimationHeader);

            if (!hideAnimationPreview && animEditor != null && Event.current.type != EventType.Repaint)
            {
                animEditor.DrawHeader();
                animEditor.OnInspectorGUI();
            }
        }
    }


    public override bool HasPreviewGUI()
    {
        return !disableAnimPreview && !hideAnimationPreview;
    }

    private void OnDisable()
    {
        if (animEditor != null)
        {
            DestroyImmediate(animEditor);
        }
    }

    public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
    {
        base.OnInteractivePreviewGUI(r, background);

        if (clip != null)
        {
            if (animEditor != null)
            {
                if (animEditor.target != clip)
                {
                    DestroyImmediate(animEditor);
                    animEditor = CreateEditor(clip);
                }
                else
                {
                    try
                    {
                        animEditor.OnInteractivePreviewGUI(r, background);
                    }
                    catch
                    {

                    }
                }
            }
            else
            {
                animEditor = CreateEditor(clip);
            }
        }
    }
}
