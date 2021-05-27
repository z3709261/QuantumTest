using System.Collections;
using Photon.Deterministic;

namespace Quantum
{
    public unsafe class JumpSurvival : BasicGameRound
    {
        public class RoundInfo
        {
            public int      Num;
            public bool     IsLeft;
            public FP       Speed;
            public FP       SleepTime;
        }

        public ArrayList roundlist = new ArrayList();

        public override void ChangeLoadMap(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.LoadMap;
            f.Global->CurrentGameRound.Timer = 0;

            var map = f.FindAsset<Map>("Resources/DB/Map/JumpSurvival/JumpSurvivalMapAsset");
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
            base.ChangeReady(f);
            f.Global->CurrentGameRound.Timer = 0;
            f.Global->CurrentGameRound.SecondRound = -1;
            f.Global->CurrentGameRound.SecondRoundTime = 0;
            f.Global->CurrentGameRound.SceneEntitys = f.AllocateList<EntityRef>();
            RoundInfo info = new RoundInfo();
            info.Num = 1;
            info.IsLeft = true;
            info.Speed = 5;
            info.SleepTime = 2;

            roundlist.Add(info);

            info = new RoundInfo();
            info.Num = 2;
            info.IsLeft = true;
            info.Speed = 5;
            info.SleepTime = 2;

            roundlist.Add(info);


            info = new RoundInfo();
            info.Num = 1;
            info.IsLeft = false;
            info.Speed = 5;
            info.SleepTime = 2;

            roundlist.Add(info);


            info = new RoundInfo();
            info.Num = 3;
            info.IsLeft = false;
            info.Speed = 5;
            info.SleepTime = 2;

            roundlist.Add(info);
        }

        public void StartRound(Frame f)
        {
            f.Global->CurrentGameRound.SecondRound = f.RNG->Next(0, roundlist.Count - 1);
            f.Global->CurrentGameRound.SecondRoundTime = 0;

            RoundInfo info = (RoundInfo)roundlist[f.Global->CurrentGameRound.SecondRound];
            FPVector3 Initvec = FPVector3.Zero;
            if(info.IsLeft)
            {
                Initvec = new FPVector3(0, 1, -14);
            }
            else
            {
                Initvec = new FPVector3(0, 1, 14);
            }

            var list = f.ResolveList(f.Global->CurrentGameRound.SceneEntitys);
            for(int i = 0; i < info.Num; i++)
            {
                var PrototypeRef = f.FindAsset<EntityPrototype>("Resources/DB/Map/JumpSurvival/SphereEntity|EntityPrototype");
                var p = f.Create(PrototypeRef);
                var playerTransform = f.Unsafe.GetPointer<Transform3D>(p);
                var gameSceneEntity = f.Unsafe.GetPointer<GameSceneEntity>(p);
                if (info.IsLeft == false)
                {
                    playerTransform->Rotate(0, 180, 0);
                    var paramslist = f.ResolveList(gameSceneEntity->Params);
                    paramslist[0] = 1;
                }
                playerTransform->Position = Initvec;
                playerTransform->Position.Z = playerTransform->Position.Z + i * 6;

                list.Add(p);
            }
        }

        public void UpdateRound(Frame f)
        {
            var list = f.ResolveList(f.Global->CurrentGameRound.SceneEntitys);
            if(list.Count > 0)
            {
                RoundInfo info = (RoundInfo)roundlist[f.Global->CurrentGameRound.SecondRound];
                for (int i = 0; i < list.Count; i++)
                {
                    var transform3D = f.Unsafe.GetPointer<Transform3D>(list[i]);
                    if (transform3D == default)
                    {
                        return;
                    }

                    transform3D->Position = transform3D->Position + transform3D->Forward * info.Speed * f.DeltaTime;
                }
            }
            else
            {
                f.Global->CurrentGameRound.SecondRound = -1;
            }
        }
        public override void RunLogic(Frame f)
        {
            base.RunLogic(f);
            f.Global->CurrentGameRound.Timer = f.Global->CurrentGameRound.Timer + f.DeltaTime;

            if(f.Global->CurrentGameRound.Timer >= 60)
            {
                ChangeFinish(f);
            }
            else
            {
                if (f.Global->CurrentGameRound.SecondRound == -1)
                {
                    StartRound(f);
                }
                else
                {
                    UpdateRound(f);
                }
            }
        }


        public override void UpdatePlayerInput(Frame f)
        {
            var robotsFilter = f.Filter<Transform3D, PlayerFields, PhysicsBody3D>();
            while (robotsFilter.NextUnsafe(out var robot, out var transform, out var playerFields, out var physicsBody3D))
            {
                var input = f.GetPlayerInput(playerFields->OwnerID);
            }
        }

        public override void OnTrigger3D(Frame f, TriggerInfo3D info)
        {

            var gameSceneTrigger = f.Unsafe.GetPointer<GameSceneTrigger>(info.Other);
            var gameSceneEntity = f.Unsafe.GetPointer<GameSceneEntity>(info.Entity);
            if (gameSceneTrigger != default && gameSceneEntity != default)
            {
                Log.Error("Other :" + info.Other);
                Log.Error("Entity :" + info.Entity);

                var Triggerparamslist = f.ResolveList(gameSceneTrigger->Params);
                var paramslist = f.ResolveList(gameSceneEntity->Params);
                if(Triggerparamslist[0] != paramslist[0])
                {
                    var list = f.ResolveList(f.Global->CurrentGameRound.SceneEntitys);
                    list.Remove(info.Entity);
                    f.Destroy(info.Entity);
                }

            }
        }
    }
}
