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

[CreateAssetMenu(menuName = "Quantum/GameAnimatorState", order = Quantum.EditorDefines.AssetMenuPriorityStart + 156)]
public partial class GameAnimatorStateAsset : AssetBase {
  public Quantum.GameAnimatorState Settings;

  public override Quantum.AssetObject AssetObject => Settings;
  
  public override void Reset() {
    if (Settings == null) {
      Settings = new Quantum.GameAnimatorState();
    }
    base.Reset();
  }
}

public static partial class GameAnimatorStateAssetExts {
  public static GameAnimatorStateAsset GetUnityAsset(this GameAnimatorState data) {
    return data == null ? null : UnityDB.FindAsset<GameAnimatorStateAsset>(data);
  }
}
