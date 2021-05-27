using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A StateMachineBehaviour assigned to states that handles assigning the state as well as any behaviours it utilizes.
/// </summary>
public class GameASBContainer : StateMachineBehaviour
{
#if UNITY_EDITOR

    public GameAnimatorStateAsset stateAsset;

    public Object stateAssetDirectory = null;

    [HideInInspector()]
    public List<CustomAnimatorBehaviourAsset> behaviourAssets;

    public void OnValidate()
    {
        if (stateAsset != null)
        {
            if (stateAsset.animatorState == null)
            {
                var path = AssetDatabase.GetAssetPath(this);
                var objs = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int i = 0; i < objs.Length; i++)
                {
                    if (objs[i] is UnityEditor.Animations.AnimatorState)
                    {
                        var state = objs[i] as UnityEditor.Animations.AnimatorState;
                        for (int j = 0; j < state.behaviours.Length; j++)
                        {
                            if (state.behaviours[j] == this)
                            {
                                stateAsset.animatorState = state;
                                stateAsset.stateBehaviours = new List<CustomAnimatorBehaviourAsset>(behaviourAssets.ToArray());
                                return;
                            }
                        }
                    }
                }
            }
            else
            {
                stateAsset.stateBehaviours = new List<CustomAnimatorBehaviourAsset>(behaviourAssets);
            }
        }
        else if (stateAssetDirectory != null)
        {


            var objs = Selection.objects;
            var stateName = string.Empty;
            foreach (var obj in objs)
            {
                if (obj is UnityEditor.Animations.AnimatorState)
                {
                    var s = (UnityEditor.Animations.AnimatorState)obj;
                    stateName = s.name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogWarning("No state found.");
                return;
            }

            GameAnimatorStateAsset newState = CreateInstance<GameAnimatorStateAsset>();
            var path = AssetDatabase.GetAssetPath(stateAssetDirectory);

            newState.name = stateName;

            AssetDatabase.CreateAsset(newState, path + "/" + newState.name + ".asset");

            AssetDatabase.Refresh();

            stateAsset = newState;

            EditorUtility.SetDirty(this);

            AssetDatabase.Refresh();
        }
    }

    [ContextMenu("Validate")]
    void Refresh()
    {
        OnValidate();
    }

#endif
}
