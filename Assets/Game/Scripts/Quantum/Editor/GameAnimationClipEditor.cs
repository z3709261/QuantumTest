using System.Collections;
using System.Collections.Generic;
using Photon.Deterministic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneObjAnimationClipAsset))]
public class SceneObjAnimationClipEditor : Editor
{

    private SceneObjAnimationClipAsset asset = null;
    // Start is called before the first frame update
    private void OnEnable()
    {
        asset = (SceneObjAnimationClipAsset)target;
    }

    public override void OnInspectorGUI()
    {
        var clip = asset.Settings;

        if (GUILayout.Button("Import Animator"))
        {
            ExportSceneObjAnimationClip.CreateAsset(asset, (AnimationClip)asset.clip);

            EditorUtility.SetDirty(asset);
            AssetDatabase.Refresh();
        }

        base.OnInspectorGUI();

    }
}
