using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
    public unsafe class PlayerManagerSystem : SystemMainThread, ISignalOnPlayerDataSet
    {
        public override void OnInit(Frame f)
        {
            f.Global->PlayerList = f.AllocateList<GamePlayer>();
        }

        public override void Update(Frame f)
        {
            var hitboxFilter = f.Filter<HitBox>();

            var PlayerFieldsFilter = f.Filter<PlayerFields>();

            var list = f.ResolveList(f.Global->PlayerList);

            while (hitboxFilter.Next(out EntityRef e, out HitBox hb))
            {
                if (hb.IsActive == false)
                    continue;
                while (PlayerFieldsFilter.Next(out EntityRef e1, out PlayerFields pf))
                {
                    if (e1 == hb.attacker)
                    {
                        continue;
                    }

                    var hurtBoxList = f.ResolveList(pf.hurtBoxList);

                    for (int h = 0; h < hurtBoxList.Count && pf.hitByAttack == false; h++)
                    {
                        if (hb.bound.Intersects(hurtBoxList[h]))
                        {
                            if (f.Unsafe.TryGetPointer(e1, out PlayerFields* targetFighter))
                            {
                                targetFighter->hitByAttack = true;
                                //targetFighter->hitPosition = FP._0_50 * (hb.bound.Center + hurtBoxList[h].Center);
                                f.Signals.OnHit(hb.attacker, e1);

                                f.Destroy(e);
                            }                                
                            break;
                        }
                    }
                }
                /*for (int i = 0; i < list.Count; i++)
                {
                    GamePlayer* playerptr = list.GetPointer(i);
                    if (playerptr->PlayerEntity == hb.attacker)
                    {
                        continue;
                    }

                    if (f.Unsafe.TryGetPointer(playerptr->PlayerEntity, out PlayerFields* targetFighter))
                    {
                        var hurtBoxList = f.ResolveList(targetFighter->hurtBoxList);

                        for (int h = 0; h < hurtBoxList.Count && targetFighter->hitByAttack == false; h++)
                        {
                            if (hb.bound.Intersects(hurtBoxList[h]))
                            {
                                f.Signals.OnHit(hb.attacker, playerptr->PlayerEntity);
                                targetFighter->hitByAttack = true;
                                targetFighter->hitPosition = FP._0_50 * (hb.bound.Center + hurtBoxList[h].Center);

                                f.Destroy(e);
                                break;
                            }
                        }
                    }
                }*/
            }
        }

        public void OnPlayerDataSet(Frame f, PlayerRef playerRef)
        {
            var playerData = f.GetPlayerData(playerRef);
            var list = f.ResolveList(f.Global->PlayerList);
            for (int i = 0; i < list.Count; i++)
            {
                if(list[i].Player == playerRef)
                {
                    GamePlayer* playerPtr = list.GetPointer(i);
                    playerPtr->IsLoadMap = playerData.IsLoadMap;
                    playerPtr->IsLoadScene = playerData.IsLoadScene;
                    playerPtr->IsLoadPlayer = playerData.IsLoadPlayer;
                    playerPtr->IsEnter = playerData.IsEnter;
                    playerPtr->IsFinish = playerData.IsFinish;
                    return;
                }
            }

            GamePlayer newplayer = new GamePlayer();
            newplayer.Player = playerRef;
            newplayer.roleid = playerData.roleid;
            newplayer.rolename = playerData.rolename;
            newplayer.headid = playerData.headid;
            newplayer.avaterId = playerData.avaterId;
            newplayer.hairId = playerData.hairId;
            newplayer.hairColor = playerData.hairColor;
            newplayer.bodyColor = playerData.bodyColor;
            newplayer.IsLoadMap = playerData.IsLoadMap;
            newplayer.IsLoadScene = playerData.IsLoadScene;
            newplayer.IsLoadPlayer = playerData.IsLoadPlayer;
            newplayer.IsEnter = playerData.IsEnter;
            newplayer.IsFinish = playerData.IsFinish;
            list.Add(newplayer);

            /*var playerEntity = playerData.entityRef;
            var transform = f.Unsafe.GetPointer<Transform3D>(playerEntity);

            transform->Position = new FPVector3(0,12, 0);

            f.Events.OnGamePlayerUpdate(playerRef);*/
            /*
                        for (int playerID = 0; playerID < f.Global->PlayerList.Length; playerID++)
                        {
                            var playerData = f.GetPlayerData(playerID);
                            if (playerData != null)
                            {
                                Log.Error("palyercount: " + f.PlayerCount + " playerRef:" + playerID);
                            }
                        }*/


            //var prototypeAsset = f.FindAsset<EntityPrototype>(playerData.PrototypeRef.Id.Value);
            //var character = f.Create(prototypeAsset);

        }

    }
}
