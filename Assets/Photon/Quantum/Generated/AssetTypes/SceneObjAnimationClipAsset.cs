// <auto-generated>
// This code was auto-generated by a tool, every time
// the tool executes this code will be reset.
//
// If you need to extend the classes generated to add
// fields or methods to them, please create partial  
// declarations in another file.
// </auto-generated>

using Quantum;
using UnityEngine;

[CreateAssetMenu(menuName = "Quantum/SceneObjAnimationClip", order = Quantum.EditorDefines.AssetMenuPriorityStart + 468)]
public partial class SceneObjAnimationClipAsset : AssetBase {
  public Quantum.SceneObjAnimationClip Settings;

  public override Quantum.AssetObject AssetObject => Settings;
  
  public override void Reset() {
    if (Settings == null) {
      Settings = new Quantum.SceneObjAnimationClip();
    }
    base.Reset();
  }
}

public static partial class SceneObjAnimationClipAssetExts {
  public static SceneObjAnimationClipAsset GetUnityAsset(this SceneObjAnimationClip data) {
    return data == null ? null : UnityDB.FindAsset<SceneObjAnimationClipAsset>(data);
  }
}
