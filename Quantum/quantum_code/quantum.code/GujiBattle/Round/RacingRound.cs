using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Deterministic;

namespace Quantum
{
    public unsafe class RacingRound: BasicGameRound
    {
        FP fMoveSpeed = FP.FromFloat_UNSAFE(10.0f);

        public override void ChangeLoadMap(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.LoadMap;
            f.Global->CurrentGameRound.Timer = 0;

            var map = f.FindAsset<Map>("Resources/DB/Map/Racing/RacingMapAsset");
            if (f.Map == map)
            {
                f.Events.OnGameRoundLoadMap();
            }
            else
            {
                f.Map = map;
            }

        }

        public override void RunLogic(Frame f)
        {
            base.RunLogic(f);
            f.Global->CurrentGameRound.Timer = f.Global->CurrentGameRound.Timer + f.DeltaTime;
        }


        public override void UpdatePlayerInput(Frame f)
        {
            var robotsFilter = f.Filter<Transform3D, PlayerFields, PhysicsBody3D>();
            while (robotsFilter.NextUnsafe(out var robot, out var transform, out var playerFields, out var physicsBody3D))
            {
                var input = f.GetPlayerInput(playerFields->OwnerID);

                if (input->LeftButton.WasPressed && playerFields->LastLeftButton == false)
                {
                    playerFields->LastLeftButton = true;
                    physicsBody3D->AddForce(transform->Forward * fMoveSpeed);
                }

                if (input->RightButton.WasPressed && playerFields->LastLeftButton == true)
                {
                    playerFields->LastLeftButton = false;
                    physicsBody3D->AddForce(transform->Forward * fMoveSpeed);
                }

                //kcc->Move(f, robot, transform->Forward, this, -1, transform, null, f.DeltaTime);
            }
        }

        public override void OnTrigger3D(Frame f, TriggerInfo3D info)
        {
            var fields = f.Unsafe.GetPointer<PlayerFields>(info.Entity);
            if (fields == default)
            {
                return;
            }

            var body = f.Unsafe.GetPointer<PhysicsCollider3D>(info.Entity);
            body->Enabled = false;

            var list = f.ResolveList(f.Global->CurrentGameRound.EndPlayers);
            list.Add(fields->OwnerID);

            f.Events.OnGameRoundPlayerEnd(list.Count, fields->OwnerID);

            Log.Error("OnTrigger3D " + fields->OwnerID);

            var roomPlayerlist = f.ResolveList(f.Global->PlayerList);

            if (list.Count >= (roomPlayerlist.Count - 1))
            {
                ChangeFinish(f);
            }
        }
    }
}
