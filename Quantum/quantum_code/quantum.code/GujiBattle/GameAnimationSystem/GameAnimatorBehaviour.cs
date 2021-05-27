using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quantum
{
    public abstract unsafe class GameAnimatorBehaviour : CustomAnimatorBehaviour
    {
        public override unsafe void OnEnter(Frame f, EntityRef entity, CustomAnimator* animator)
        {
            GetPlayerTransform(f, entity, out Transform3D* transform);
            OnEnter(f, entity, animator, transform);
        }

        private static void GetPlayerTransform(Frame f, EntityRef entity, out Transform3D* transform)
        {
            transform = f.Unsafe.GetPointer<Transform3D>(entity);
        }

        public override unsafe bool OnUpdate(Frame f, EntityRef entity, CustomAnimator* animator)
        {
            GetPlayerTransform(f, entity, out Transform3D* transform);
            return OnUpdate(f, entity, animator, transform);
        }

        public override unsafe void OnExit(Frame f, EntityRef entity, CustomAnimator* animator)
        {
            GetPlayerTransform(f, entity, out Transform3D* transform);
            OnExit(f, entity, animator, transform);
        }

        public abstract void OnEnter(Frame frame, EntityRef playerRef, CustomAnimator* animator, Transform3D* transform);
        public abstract bool OnUpdate(Frame frame, EntityRef playerRef, CustomAnimator* animator, Transform3D* transform);
        public abstract void OnExit(Frame frame, EntityRef playerRef, CustomAnimator* animator, Transform3D* transform);
    }
}
