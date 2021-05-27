using Photon.Deterministic;
using Quantum;
using System;
using UnityEngine;

public class QuantumStaticSphereCollider3D : MonoBehaviour {
  public FP Radius;
  public FPVector3 PositionOffset;
  public QuantumStaticColliderSettings Settings;

  void OnDrawGizmos() {
    DrawGizmo(false);
  }

  void OnDrawGizmosSelected() {
    DrawGizmo(true);
  }

  void DrawGizmo(Boolean selected) {
    // the radius with which the sphere with be baked into the map
    var radius = Radius.AsFloat * transform.localScale.x;
    
    GizmoUtils.DrawGizmosSphere(transform.position + PositionOffset.ToUnityVector3(), radius, selected, QuantumEditorSettings.Instance.StaticColliderColor);
  }
}
