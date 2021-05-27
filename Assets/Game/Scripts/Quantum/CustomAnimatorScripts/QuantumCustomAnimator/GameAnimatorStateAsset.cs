using UnityEngine;
using Quantum;
using System.Collections.Generic;
using Photon.Deterministic;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

/// <summary>
/// Extended information about state assets used in the game.
/// </summary>
public partial class GameAnimatorStateAsset
{
#if UNITY_EDITOR
    public AnimatorState animatorState;
    public List<CustomAnimatorBehaviourAsset> stateBehaviours;

    [HideInInspector()]
    public string[] nameList;
#endif

#if UNITY_EDITOR

    void OnValidate()
    {
        if (Settings.hurtBoxSets.Count == 0)
        {
            Settings.hurtBoxSets = new List<HurtBoxSet>() { new HurtBoxSet() { frameIndex = 0, hurtBoxes = new List<FPBounds3>() } };
        }

        nameList = new string[stateBehaviours.Count];
        for (int i = 0; i < stateBehaviours.Count; i++)
        {
            nameList[i] = stateBehaviours[i]?.name;

        }

        Settings.behaviours.Clear();
        for (int i = 0; i < stateBehaviours.Count; i++)
        {
            if (stateBehaviours[i] == null)
                continue;
            Settings.behaviours.Add((AssetRefCustomAnimatorBehaviour)stateBehaviours[i].AssetObject);
        }
    }

#endif
}