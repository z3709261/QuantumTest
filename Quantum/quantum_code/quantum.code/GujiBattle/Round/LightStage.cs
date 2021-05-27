using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
    public unsafe class LightStage : BasicGameRound
    {
        private List<EntityRef> CollisionList = new List<EntityRef>();

        private int HitForce = 500;

        private int[] FirstRoundTitle = new int[16] {0, 1, 2, 3, 4, 5, 9, 10, 14, 15, 19, 20, 21, 22, 23, 24};

        private int[] SecondRoundTitle = new int[8] { 6, 7, 8, 11, 13, 16, 17, 18 };

        public override void ChangeLoadMap(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.LoadMap;
            f.Global->CurrentGameRound.Timer = 0;

            var map = f.FindAsset<Map>("Resources/DB/Map/DangerousTurntable/DangerousTurntableMapAsset");
            if (f.Map == map)
            {
                f.Events.OnGameRoundLoadMap();
            }
            else
            {
                f.Map = map;
            }

        }

        public override void ChangeReady(Frame f)
        {
            foreach (var (entity, dynamicCollider) in f.Unsafe.GetComponentBlockIterator<Dynamic_Collider>())
            {
                CollisionList.Add(entity);
            }
            base.ChangeReady(f);

            f.Global->CurrentGameRound.Timer = 30;
        }

        public void StartSecondRound(Frame f)
        {
            f.Global->CurrentGameRound.SecondRound = f.Global->CurrentGameRound.SecondRound + 1;
            f.Global->CurrentGameRound.SecondRoundState = SecondRoundState.Run;
            int[] Tille = null;
            if (f.Global->CurrentGameRound.SecondRound == 1)
            {
                Tille = FirstRoundTitle;

                var animatorsIterator = f.Unsafe.GetComponentBlockIterator<CustomAnimator>();
                var e = animatorsIterator.GetEnumerator();
                while (e.MoveNext())
                {
                    CustomAnimator.SetInteger(f, e.Current.Component, "Round", 1);
                }
            }
            else if (f.Global->CurrentGameRound.SecondRound == 2)
            {
                Tille = SecondRoundTitle;
            }

            for(int i = 0; i < Tille.Length; i++)
            {
                var gameEntityAnimation = f.Unsafe.GetPointer<SceneObjEntityAnimation>(CollisionList[Tille[i]]);
                gameEntityAnimation->PlayTime = 0;
                var transform3D = f.Unsafe.GetPointer<Transform3D>(CollisionList[Tille[i]]);
                gameEntityAnimation->InitPosition = transform3D->Position;
                gameEntityAnimation->InitRotate = transform3D->Rotation;
                gameEntityAnimation->nowClipIndex = 1;
            }
        }
        public override void RunLogic(Frame f)
        {
            f.Global->CurrentGameRound.Timer -= f.DeltaTime;
            UpdatePlayerInput(f);
            if (f.IsVerified)
            {
                if (f.Global->CurrentGameRound.SecondRound == 0 && f.Global->CurrentGameRound.Timer <= 25)
                {
                    StartSecondRound(f);
                }
                else if (f.Global->CurrentGameRound.SecondRound == 1 && f.Global->CurrentGameRound.Timer <= 15)
                {
                    StartSecondRound(f);
                }

                if(f.Global->CurrentGameRound.Timer <= 0)
                {
                    f.Global->CurrentGameRound.SecondRound = 2;

                    ChangeFinish(f);
                }
            }
        }


        public override void UpdatePlayerInput(Frame f)
        {
            var robotsFilter = f.Filter<Transform3D, PlayerFields, PhysicsBody3D>();
            while (robotsFilter.NextUnsafe(out var robot, out var transform, out var playerFields, out var physicsBody3D))
            {
                var input = f.GetPlayerInput(playerFields->OwnerID);

                var animator = f.Unsafe.GetPointer<CustomAnimator>(robot);
                var state = CustomAnimator.GetCurrentState(f, animator);
                if (state != null)
                {

                    var s = f.Assets.GameAnimatorState(state.StateAsset);
                    int statusid = CustomAnimator.GetInteger(f, animator, "status");
                    if (input->Direction.XOY != default)
                    {
                        if (s != null && s.canMove)
                        {
                            transform->Position += (input->Direction.XOY * playerFields->MoveSpeed) * f.DeltaTime;

                            transform->Rotation = FPQuaternion.LookRotation(
                               input->Direction.XOY,
                               FPVector3.Up
                             );
                            if(statusid != 2)
                            {
                                CustomAnimator.SetInteger(f, animator, "status", 2);
                            }                            
                        }
                    }
                    else
                    {
                        if (statusid == 2 )
                        {
                            CustomAnimator.SetInteger(f, animator, "status", 0);
                        }
                    }

                    if (input->PickThrowButton.WasPressed)
                    {
                        if (s != null && s.canAttack)
                        {
                            CustomAnimator.SetInteger(f, animator, "status", 4);
                        }
                    }

                }
               

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

        public override void OnHit(Frame f, EntityRef AttackEntity, EntityRef HitEntity)
        {

            var Hitfields = f.Unsafe.GetPointer<PlayerFields>(HitEntity);

            var Attackfields = f.Unsafe.GetPointer<PlayerFields>(AttackEntity);

            if (Hitfields == default || Attackfields == default)
            {
                return;
            }
            
            Hitfields->hitByAttack = false;
            var AttackTransform = f.Unsafe.GetPointer<Transform3D>(AttackEntity);
            var HitTransform = f.Unsafe.GetPointer<Transform3D>(HitEntity);
            var physicsBody3D = f.Unsafe.GetPointer<PhysicsBody3D>(HitEntity);

            physicsBody3D->AddForce((HitTransform->Position - AttackTransform->Position).Normalized * HitForce);
            var animator = f.Unsafe.GetPointer<CustomAnimator>(HitEntity);
            CustomAnimator.SetInteger(f, animator, "status", 5);
        }
    }
}
