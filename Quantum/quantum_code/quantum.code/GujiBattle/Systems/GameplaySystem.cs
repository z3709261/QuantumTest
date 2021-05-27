using Photon.Deterministic;

namespace Quantum
{
    public unsafe class GameplaySystem : SystemMainThread, ISignalOnMapChanged, ISignalOnTrigger3D, ISignalOnTrigger2D, ISignalOnHit
    {
        private BasicGameRound curRound = null;

        private int CurIndex = 0;

        private void InitCurRound(Frame f, int index)
        {
            int rand = f.RNG->Next(1, 100);
            f.Global->CurrentGameRound.RoundIndex = index;
            f.Global->CurrentGameRound.GameType = 3;
            f.Global->CurrentGameRound.EndPlayers = f.AllocateList<PlayerRef>();
            //curRound = new JumpSurvival();
            curRound = new LightStage();
            //curRound = new RacingRound();
            curRound.Init(f);


        }
        public override void OnInit(Frame f)
        {
            CurIndex = 1;
            InitCurRound(f, CurIndex);
        }

        public override void Update(Frame f)
        {
            if(curRound != null)
            {                
                curRound.Update(f);
                if(f.IsVerified)
                {
                    if (f.Global->CurrentGameRound.State == RoundState.Ended)
                    {
                        curRound.Destory(f);
                        curRound = null;

                        CurIndex = CurIndex + 1;
                        f.FreeList<PlayerRef>(f.Global->CurrentGameRound.EndPlayers);
                        f.Global->CurrentGameRound.State = RoundState.LoadMap;
                        if (CurIndex <= 3)
                        {
                            InitCurRound(f, CurIndex);
                        }
                        else
                        {
                            f.Events.OnGameEnd();
                        }
                    }
                }
                
            }
        }

        public void OnTrigger3D(Frame f, TriggerInfo3D info)
        {
            if (curRound != null)
            {
                curRound.OnTrigger3D(f, info);
            }
        }

        public void OnTrigger2D(Frame frame, TriggerInfo2D info)
        {
            if (curRound != null)
            {
                curRound.OnTrigger2D(frame, info);
            }
        }

        void ISignalOnMapChanged.OnMapChanged(Frame f, AssetRefMap previousMap)
        {
            if (curRound != null)
            {
                Log.Error("OnMapChanged");
                curRound.OnMapChanged(f, previousMap);
            }
        }

        public void OnHit(Frame f, EntityRef AttackEntity, EntityRef HitEntity)
        {
            if (curRound != null)
            {
                curRound.OnHit(f, AttackEntity, HitEntity);
            }
        }

    }
}