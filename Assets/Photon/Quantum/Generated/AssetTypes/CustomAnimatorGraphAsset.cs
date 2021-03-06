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

[CreateAssetMenu(menuName = "Quantum/CustomAnimatorGraph", order = Quantum.EditorDefines.AssetMenuPriorityStart + 52)]
public partial class CustomAnimatorGraphAsset : AssetBase {
  public Quantum.CustomAnimatorGraph Settings;

  public override Quantum.AssetObject AssetObject => Settings;
  
  public override void Reset() {
    if (Settings == null) {
      Settings = new Quantum.CustomAnimatorGraph();
    }
    base.Reset();
  }
}

public static partial class CustomAnimatorGraphAssetExts {
  public static CustomAnimatorGraphAsset GetUnityAsset(this CustomAnimatorGraph data) {
    return data == null ? null : UnityDB.FindAsset<CustomAnimatorGraphAsset>(data);
  }
}
