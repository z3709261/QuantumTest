using Photon.Deterministic;

namespace Quantum
{
    public unsafe class CustomAnimatorStateSystem : SystemSignalsOnly, ISignalOnAnimatorStateEnter, ISignalOnAnimatorStateUpdate, ISignalOnAnimatorStateExit
    {
        public void OnAnimatorStateEnter(Frame f, EntityRef entity, CustomAnimator* animator)
        {
            var state = CustomAnimator.GetCurrentState(f, animator);

            if (state != null)
            {
                var stateAsset = f.Assets.GameAnimatorState(state.StateAsset);
                for (int i = 0; i < stateAsset.behaviours.Count; i++)
                {
                    f.Assets.CustomAnimatorBehaviour(stateAsset.behaviours[i]).OnEnter(f, entity, animator);
                }
            }
        }

        public void OnAnimatorStateUpdate(Frame f, EntityRef entity, CustomAnimator* animator)
        {
            var state = CustomAnimator.GetCurrentState(f, animator);

            if (state != null)
            {
                var stateAsset = f.Assets.GameAnimatorState(state.StateAsset);
                stateAsset.OnUpdate(f, f.Unsafe.GetPointer<PlayerFields>(entity),
                    f.Unsafe.GetPointer<Transform3D>(entity), animator);

                for (int i = 0; i < stateAsset.behaviours.Count; i++)
                {
                    if (f.Assets.CustomAnimatorBehaviour(stateAsset.behaviours[i]).OnUpdate(f, entity, animator))
                        break;
                }
            }
        }

        public void OnAnimatorStateExit(Frame f, EntityRef entity, CustomAnimator* animator)
        {
            var state = CustomAnimator.GetCurrentState(f, animator);
            if (state != null)
            {
                var stateAsset = f.Assets.GameAnimatorState(state.StateAsset);

                for (int i = 0; i < stateAsset.behaviours.Count; i++)
                {
                    f.Assets.CustomAnimatorBehaviour(stateAsset.behaviours[i]).OnExit(f, entity, animator);
                }
            }
        }
    }
}