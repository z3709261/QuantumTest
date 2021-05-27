using Photon.Deterministic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quantum
{
    [System.Serializable()]
    public unsafe partial class GameAttackState : GameAnimatorBehaviour
    {
        public int startFrame;
        public int endFrame;
        public FPBounds3 bounds3D;
        
        public override unsafe void OnEnter(Frame frame, EntityRef playerRef, CustomAnimator* animator, Transform3D* transform)
        {
            var hitBoxEntity = frame.Create();
            var hitBoxComponent = new HitBox();

            hitBoxComponent.IsActive = false;
            hitBoxComponent.attacker = playerRef;

            frame.Set(hitBoxEntity, hitBoxComponent);

            //frame.Events.PlayAudioEffect(transform->Position.XY, frame.RuntimeConfig.whiffSFX);
        }

        public override unsafe void OnExit(Frame frame, EntityRef playerRef, CustomAnimator* animator, Transform3D* transform)
        {
            var filter = frame.Filter<HitBox>();
            while (filter.NextUnsafe(out EntityRef e, out HitBox* box))
            {
                if (box->attacker != playerRef)
                    continue;
                frame.Destroy(e);
            }
            CustomAnimator.SetInteger(frame, animator, "status", 0);
        }

        public override unsafe bool OnUpdate(Frame frame, EntityRef playerRef, CustomAnimator* a, Transform3D* transform)
        {
            FP time = CustomAnimator.GetActiveWorldTime(frame, a);

            if (time * 60 >= startFrame && time * 60 <= endFrame)
            {
                var tt = frame.Get<Transform3D>(playerRef);

                var filter = frame.Filter<HitBox>();
                while (filter.NextUnsafe(out EntityRef e, out HitBox* box))
                {
                    if (box->attacker != playerRef)
                        continue;

                    box->IsActive = true;                   

                    box->bound = bounds3D;
                    box->bound.Center = (tt.Position + (tt.Rotation * bounds3D.Center));
                }


            }
            else
            {
                var filter = frame.Filter<HitBox>();
                while (filter.NextUnsafe(out EntityRef e, out HitBox* box))
                {
                    if (box->attacker != playerRef)
                        continue;

                    box->IsActive = false;
                }
            }

            return false;
        }
    }
}