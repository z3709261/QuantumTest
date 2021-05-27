using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quantum
{
    [System.Serializable()]
    public unsafe partial class GameHitState : GameAnimatorBehaviour
    {
        public override unsafe void OnEnter(Frame frame, EntityRef playerRef, CustomAnimator* animator, Transform3D* transform)
        {
        }

        public override unsafe void OnExit(Frame frame, EntityRef playerRef, CustomAnimator* animator, Transform3D* transform)
        {
            CustomAnimator.SetInteger(frame, animator, "status", 0);
        }

        public override unsafe bool OnUpdate(Frame frame, EntityRef playerRef, CustomAnimator* a, Transform3D* transform)
        {
            return false;
        }
    }
}
