using Photon.Deterministic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quantum
{
    [System.Serializable()]
    public class HurtBoxSet
    {
        public int frameIndex;
        public List<FPBounds3> hurtBoxes;
    }

    public unsafe partial class GameAnimatorState
    {
        /// <summary>
        /// Is this a crouching state?
        /// </summary>
        public bool isCrouching;

        /// <summary>
        /// Should rotation snap or change sides quickly?
        /// </summary>
        public bool snapRotation = true;

        public string stateName;

        public FPBounds3 mainExtents;

        public List<HurtBoxSet> hurtBoxSets;

        public List<AssetRefCustomAnimatorBehaviour> behaviours;

        public bool canMove;
        public bool canAttack;

        public void GetHurtBoxList(int frame, out List<FPBounds3> result)
        {
            result = hurtBoxSets[0].hurtBoxes;
            for (int i = 0; i < hurtBoxSets.Count; i++)
            {
                result = hurtBoxSets[i].hurtBoxes;
                if (frame < hurtBoxSets[i].frameIndex)
                    break;
            }
        }
        
        internal void OnUpdate(Frame f, PlayerFields* player, Transform3D* transform, CustomAnimator* animator)
        {
            player->mainExtents = mainExtents;
            player->mainExtents.Center += transform->Position;

            var hurtBoxList = f.ResolveList(player->hurtBoxList);

            FP time = CustomAnimator.GetActiveWorldTime(f, animator);

            GetHurtBoxList(FPMath.FloorToInt(time * 60), out var hurtBounds);

            for (int i = 0; i < hurtBounds.Count; i++)
            {
                if (hurtBoxList.Count <= i)
                    hurtBoxList.Add(new FPBounds3());
                var item = hurtBoxList[i];
                item.Center = (transform->Position + transform->Rotation * hurtBounds[i].Center);
                item.Extents = hurtBounds[i].Extents;
                hurtBoxList[i] = item;
            }

            while (hurtBoxList.Count > hurtBounds.Count)
                hurtBoxList.RemoveAt(hurtBoxList.Count - 1);
        }
    }
}