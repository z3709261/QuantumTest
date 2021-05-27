using Photon.Deterministic;

namespace Quantum
{
    [System.Serializable()]
    public unsafe partial class GameSetSpeedState : GameAnimatorBehaviour
    {
        public bool setOnEnter;
        public SetSpeedInfo entrySpeedInfo;

        [System.Serializable()]
        public struct SetSpeedInfo
        {
            public bool setSpeed;
            public FP Speed;

            public bool setGravity;
            public FP gravity;

            public bool calculateWithJumpHeight;
            public FP jumpHeight;
            public FP jumpTime;

            internal unsafe void Define(PlayerFields* playerFields)
            {
                if (setSpeed)
                {
                    playerFields->MoveSpeed = Speed;
                }
                if (setGravity)
                {
                    playerFields->Gravity = gravity;
                }
            }
        }

        public override unsafe void OnEnter(Frame frame, EntityRef playerRef, CustomAnimator* animator, Transform3D* transform)
        {
            if (setOnEnter)
            {
                var fields = frame.Unsafe.GetPointer<PlayerFields>(playerRef);
                if (fields == default)
                {
                    return;
                }

                entrySpeedInfo.Define(fields);
            }
        }

        public override unsafe bool OnUpdate(Frame frame, EntityRef playerRef, CustomAnimator* a, Transform3D* transform)
        {
            return false;
        }

        public override unsafe void OnExit(Frame frame, EntityRef playerRef, CustomAnimator* animator, Transform3D* transform)
        {
            var triggeredList = frame.ResolveList(animator->TriggeredStateBehaviours);
            triggeredList.Remove(Guid);
        }
    }
}
