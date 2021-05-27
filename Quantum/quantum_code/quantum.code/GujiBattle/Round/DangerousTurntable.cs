using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Deterministic;

namespace Quantum
{
    public unsafe class DangerousTurntable : BasicGameRound
    {
        public class AngleCurve
        {
            public FP fTime = 0;
            public FP fPer = 0;
        };

        private enum eTurnTableState
        {
            Ready = 0,
            Run = 1,
            EndAnimation = 2,
            End = 3,
        }
        private FP fMoveSpeed = FP.FromFloat_UNSAFE(5.0f);

        private List<EntityRef> CollisionList = new List<EntityRef>();

        private List<EntityRef> TriggerList = new List<EntityRef>();

        private int TurnNum = 0;

        private const int MaxTurnNum = 5;

        private int[] CurTurnIndex;

        private FP[] StartAngle;

        private FP AllRotateAngle = 0;

        public List<AngleCurve> AngleCurveList = new List<AngleCurve>();

        private eTurnTableState TurningState = eTurnTableState.Ready;

        public void ClearTrigger(Frame f)
        {
            for(int i = 0; i < TriggerList.Count; i++)
            {
                f.Destroy(TriggerList[i]);
            }
            TriggerList.Clear();
        }

        public void UpdateTrigger(Frame f, EntityRef triggerEntity, EntityRef collisionEntity)
        {
            var TriggerTransform = f.Unsafe.GetPointer<Transform3D>(triggerEntity);

            var CollisionTransform = f.Unsafe.GetPointer<Transform3D>(collisionEntity);

            TriggerTransform->Position = CollisionTransform->Position;
        }

        public void AddTrigger(Frame f, int TerrainIndex)
        {
            var PrototypeRef = f.FindAsset<EntityPrototype>("Resources/DB/Map/DangerousTurntable/TuantableTrigger|EntityPrototype");
            var entityRef = f.Create(PrototypeRef);
            var collisionentiry = CollisionList[TerrainIndex];

            UpdateTrigger(f, entityRef, collisionentiry);

            TriggerList.Add(entityRef);
        }

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
            foreach (var (entity, dynamic_Trigger) in f.Unsafe.GetComponentBlockIterator<Dynamic_Trigger>())
            {
                var body = f.Unsafe.GetPointer<PhysicsCollider2D>(entity);
                body->IsTrigger = false;
                TriggerList.Add(entity);
            }

            AngleCurve point = new AngleCurve();
            point.fTime = 1;
            point.fPer = FP.FromFloat_UNSAFE(0.05f);
            AngleCurveList.Add(point);
                        
            point = new AngleCurve();
            point.fTime = 4;
            point.fPer = FP.FromFloat_UNSAFE(0.9f);
            AngleCurveList.Add(point);

            point = new AngleCurve();
            point.fTime = 6;
            point.fPer = 1;
            AngleCurveList.Add(point);

            TurningState = eTurnTableState.Ready;

            base.ChangeReady(f);
        }

        public void HideDynamicCollider(Frame f)
        {
            foreach (var (entity, dynamicCollider) in f.Unsafe.GetComponentBlockIterator<Dynamic_Collider>())
            {
                var body = f.Unsafe.GetPointer<Transform3D>(entity);
                body->Rotate(0, 0, 90);
            }

        }
        public void StartTurning(Frame f)
        {

            if(TurnNum >= MaxTurnNum)
            {
                EndAllTurning(f);
                return;
            }

            for (int i = 0; i < TriggerList.Count; i++)
            {
                EntityRef trigger = TriggerList[i];
                var collider2d = f.Unsafe.GetPointer<PhysicsCollider2D>(trigger);
                collider2d->IsTrigger = false;
            }

            Log.Error("StartTurning PiOver2:" + FP.PiOver2 + " PiTimes2:" + FP.PiTimes2 + " PiInv:" + FP.PiInv);
            TurnNum = TurnNum + 1;
            TurningState = eTurnTableState.Run;
            if (TurnNum <= 3)
            {
                CurTurnIndex = new int[1];
                StartAngle = new FP[1];
                CurTurnIndex[0] = f.RNG->Next(1, 4);
                AllRotateAngle = FP.FromFloat_UNSAFE((f.RNG->Next(5, 7) * FP.PiTimes2.AsFloat) /*+ CurTurnIndex[0] * 0.785393f*/);
                EntityRef trigger = TriggerList[CurTurnIndex[0]];
                var body = f.Unsafe.GetPointer<Transform2D>(trigger);
                StartAngle[0] = body->Rotation;
                f.Global->CurrentGameRound.Timer = 0;
            }
            else
            {
                CurTurnIndex = new int[2];
                StartAngle = new FP[2];
                CurTurnIndex[0] = f.RNG->Next(0, 3);
                
                EntityRef trigger = TriggerList[CurTurnIndex[0]];
                var body = f.Unsafe.GetPointer<Transform2D>(trigger);
                StartAngle[0] = body->Rotation;

                AllRotateAngle = FP.FromFloat_UNSAFE((f.RNG->Next(5, 9) * 3.14f) + CurTurnIndex[0] * 0.785f);

                CurTurnIndex[1] = f.RNG->Next(0, 3);
                while(CurTurnIndex[1] == CurTurnIndex[0])
                {
                    CurTurnIndex[1] = f.RNG->Next(1, 4);
                }

                EntityRef trigger1 = TriggerList[CurTurnIndex[1]];
                var body1 = f.Unsafe.GetPointer<Transform2D>(trigger1);
                StartAngle[1] = body1->Rotation;

                f.Global->CurrentGameRound.Timer = 0;
            }

        }

        public void EndAllTurning(Frame f)
        {
                      
        }
        FP CalculateCubicBezierPoint(FP fTimeElapsed)
        {
            FP fPerPrev = 0;
            FP fTimePrev = 0;
            AngleCurve Curve = null;
            for (int i = 0; i < AngleCurveList.Count; i++)
            {
                Curve = AngleCurveList[i];
                if (Curve.fTime >= fTimeElapsed)
                {
                    FP fTimeCurve = fTimeElapsed - fTimePrev;
                    FP AllTime = Curve.fTime - fTimePrev;

                    FP AllPer = Curve.fPer - fPerPrev;
                    return AllPer * (fTimeCurve / AllTime) + fPerPrev;
                }

                fTimePrev = Curve.fTime;
                fPerPrev = Curve.fPer;
            }
            return 1;
         }
       
         public void UpdateTurning(Frame f)
         {
            f.Global->CurrentGameRound.Timer += f.DeltaTime;

            FP Value = CalculateCubicBezierPoint(f.Global->CurrentGameRound.Timer);

            if (Value >= 1)
            {
                Value = 1;
            }
            FP angle = AllRotateAngle * Value;

            for (int i = 0; i < CurTurnIndex.Length; i++)
            {
                EntityRef trigger = TriggerList[CurTurnIndex[i]];
                var body = f.Unsafe.GetPointer<Transform2D>(trigger);
                body->Rotation = StartAngle[i] + angle;

                if (Value >= 1)
                {
                    Value = 1;
                }
                var collider2d = f.Unsafe.GetPointer<PhysicsCollider2D>(trigger);
                collider2d->IsTrigger = true;
            }

            if(Value == 1)
            {
                TurningState = eTurnTableState.EndAnimation;
                //f.Events.OnGameEvent("DangerousTurntableRoundEnd");
            }
        }
        public override void RunLogic(Frame f)
        {
            if (TurningState == eTurnTableState.Ready)
            {
                if(f.IsVerified)
                {
                    StartTurning(f);
                }
            }
            else if (TurningState == eTurnTableState.Run)
            {
                UpdatePlayerInput(f);
                UpdateTurning(f);
            }
            else if (TurningState == eTurnTableState.EndAnimation)
            {

            }
            else if (TurningState == eTurnTableState.End)
            {
                TurningState = eTurnTableState.Ready;
            }
        }


        public override void UpdatePlayerInput(Frame f)
        {
            var robotsFilter = f.Filter<Transform3D, PlayerFields, PhysicsBody3D>();
            while (robotsFilter.NextUnsafe(out var robot, out var transform, out var playerFields, out var physicsBody3D))
            {
                var input = f.GetPlayerInput(playerFields->OwnerID);
                physicsBody3D->AddForce(input->Direction.XOY * fMoveSpeed);

                if(input->Direction.XOY != default)
                {
                    transform->Rotation = FPQuaternion.LookRotation(
                       input->Direction.XOY,
                       FPVector3.Up
                     );
                }
                
            }
        }

        public override void OnTrigger2D(Frame f, TriggerInfo2D info)
        {
            Log.Error("OnTrigger2D:" + info);
        }
    }
}
