using Photon.Deterministic;
using Quantum;
using System;
using UnityEngine;

public class QuantumStaticBoxCollider3D : MonoBehaviour {
  public FPVector3 Size;
  public FPVector3 PositionOffset;
  public FPVector3 RotationOffset;
  public QuantumStaticColliderSettings Settings;

  void OnDrawGizmos() {
    DrawGizmo(false);
  }

  void OnDrawGizmosSelected() {
    DrawGizmo(true);
  }

  void DrawGizmo(Boolean selected) {
    var t = transform;
    var matrix = Matrix4x4.TRS(
      t.position + PositionOffset.ToUnityVector3(),
      Quaternion.Euler(t.rotation.eulerAngles + RotationOffset.ToUnityVector3()),
      t.localScale);
    GizmoUtils.DrawGizmosBox(matrix, Size.ToUnityVector3(), selected, QuantumEditorSettings.Instance.StaticColliderColor);
  }
}
