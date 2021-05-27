using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Deterministic;

namespace Quantum
{
    public unsafe class BasicGameRound
    {

        public virtual void Init(Frame f)
        {
            ChangeLoadMap(f);
        }

        public virtual void Destory(Frame f)
        {
            var list = f.ResolveList(f.Global->PlayerList);
            for (int i = 0; i < list.Count; i++)
            {
                GamePlayer* playerptr = list.GetPointer(i);
                if (playerptr->PlayerEntity != default)
                {
                    playerptr->IsLoadMap = false;
                    playerptr->IsLoadScene = false;
                    playerptr->IsLoadPlayer = false;
                    playerptr->IsEnter = false;
                    playerptr->IsFinish = false;
                    playerptr->PlayerEntity = default;
                }
            }
        }

        public virtual void Update(Frame f)
        {
            switch(f.Global->CurrentGameRound.State)
            {
                case RoundState.LoadMap:
                    {
                        LoadMapLogic(f);
                    }
                    break;
                case RoundState.LoadScene:
                    {
                        LoadSceneLogic(f);
                    }
                    break;
                case RoundState.LoadPlayer:
                    {
                        LoadPlayerLogic(f);
                    }
                    break;
                case RoundState.Enter:
                    {
                        EnterLogic(f);
                    }
                    break;
                case RoundState.Ready:
                    {
                        ReadyLogic(f);
                    }
                    break;
                case RoundState.Run:
                    {
                        RunLogic(f);
                    }
                    break;
                case RoundState.Finish:
                    {
                        FinishLogic(f);
                    }
                    break;
                case RoundState.Ended:
                    {
                        EndLogic(f);
                    }
                    break;
            }
        }

        public virtual void ChangeLoadScene(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.LoadScene;
            f.Events.OnGameRoundLoadScene(f.Global->CurrentGameRound.RoundIndex, f.Global->CurrentGameRound.GameType);
        }

        public virtual void LoadSceneLogic(Frame f)
        {
            var list = f.ResolveList(f.Global->PlayerList);
            if (f.PlayerCount > list.Count)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsLoadScene == false)
                {
                    return;
                }
            }
            if (f.IsVerified)
            {
                ChangeLoadPlayer(f);
            }            
        }

        public virtual void ChangeLoadMap(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.LoadMap;

            var map = f.FindAsset<Map>("Resources/DB/Map/EmptyMapAsset");
            if(f.Map == map)
            {
                f.Events.OnGameRoundLoadMap();
            }
            else
            {
                f.Map = map;
            }
            
        }

        public virtual void LoadMapLogic(Frame f)
        {
            if(f.IsVerified)
            {
                var list = f.ResolveList(f.Global->PlayerList);
                if (f.PlayerCount > list.Count)
                {
                    return;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IsLoadMap == false)
                    {
                        return;
                    }
                }

                ChangeLoadScene(f);
            }
            
        }

        public virtual void OnMapChanged(Frame f, AssetRefMap previousMap)
        {
            Log.Error("OnMapChanged:" + previousMap);
            f.Events.OnGameRoundLoadMap();
        }

        public Transform3D GetSpawnTransform(Frame f, int index)
        {
            foreach (var (entity, spawnPoint) in f.Unsafe.GetComponentBlockIterator<SpawnPoint>())
            {
                if (spawnPoint->Index == index)
                {
                    var spawnTransform = f.Get<Transform3D>(entity);
                    return spawnTransform;
                }
            }

            return Transform3D.Create();
        }

        public virtual void ChangeLoadPlayer(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.LoadPlayer;

            var list = f.ResolveList(f.Global->PlayerList);
            for (int i = 0; i < list.Count; i++)
            {
                GamePlayer* playerptr = list.GetPointer(i);
                var PrototypeRef = f.FindAsset<EntityPrototype>("Resources/DB/Prototype/RoleEntityPrototype|EntityPrototype");
                var p = f.Create(PrototypeRef);
                var unitFields = f.Unsafe.GetPointer<PlayerFields>(p);
                unitFields->OwnerID = playerptr->Player;
                unitFields->hurtBoxList = f.AllocateList<FPBounds3>();
                playerptr->PlayerEntity = p;
                var playerTransform = f.Unsafe.GetPointer<Transform3D>(p);
                Transform3D Spawntrans = GetSpawnTransform(f, playerptr->Player);
                playerTransform->Position = Spawntrans.Position;
                playerTransform->Rotation = Spawntrans.Rotation;
            }

            f.Events.OnGameRoundLoadPlayer(f.PlayerCount);
        }


        public virtual void LoadPlayerLogic(Frame f)
        {
            var list = f.ResolveList(f.Global->PlayerList);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsLoadPlayer == false)
                {
                    return;
                }
            }
            if (f.IsVerified)
            {
                ChangeEnter(f);
            }
        }

        public virtual void ChangeEnter(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.Enter;

            f.Events.OnGameRoundEnter();
        }


        public virtual void EnterLogic(Frame f)
        {
            var list = f.ResolveList(f.Global->PlayerList);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsEnter == false)
                {
                    return;
                }
            }
            if (f.IsVerified)
            {
                ChangeReady(f);
            }
        }

        public virtual void ChangeReady(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.Ready;
            f.Global->CurrentGameRound.ReadyTime = 4;
            f.Global->CurrentGameRound.Timer = 0;

            var list = f.ResolveList(f.Global->PlayerList);

            for (int i = 0; i < list.Count; i++)
            {
                var animator = f.Unsafe.GetPointer<CustomAnimator>(list[i].PlayerEntity);
                CustomAnimator.SetInteger(f, animator, "status", 0);
            }

            f.Events.OnGameRoundReady();
        }


        public virtual void ReadyLogic(Frame f)
        {
            if (f.IsVerified)
            {
                if (f.Global->CurrentGameRound.ReadyTime > 0)
                {
                    f.Global->CurrentGameRound.ReadyTime = f.Global->CurrentGameRound.ReadyTime - f.DeltaTime;

                    if (f.Global->CurrentGameRound.ReadyTime <= 0)
                    {
                        ChangeRun(f);
                    }
                }
            }
        }

        public virtual void ChangeRun(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.Run;

            f.Events.OnGameRoundRun();
        }


        public virtual void RunLogic(Frame f)
        {
            UpdatePlayerInput(f);
        }


        public virtual void ChangeFinish(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.Finish;

            f.Events.OnGameRoundFinish();
        }


        public virtual void FinishLogic(Frame f)
        {
            var list = f.ResolveList(f.Global->PlayerList);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsFinish == false)
                {
                    return;
                }
            }
            if (f.IsVerified)
            {
                ChangeEnd(f);
            }
        }

        public virtual void ChangeEnd(Frame f)
        {
            f.Global->CurrentGameRound.State = RoundState.Ended;
            f.Events.OnGameRoundEnd();
        }


        public virtual void EndLogic(Frame f)
        {
        }

        public virtual void OnTrigger3D(Frame f, TriggerInfo3D info)
        {
        }

        public virtual void OnTrigger2D(Frame f, TriggerInfo2D info)
        {
        }

        public virtual void UpdatePlayerInput(Frame f)
        {

        }

        public virtual void OnHit(Frame f, EntityRef AttackEntity, EntityRef HitEntity)
        {

        }
    }
}
