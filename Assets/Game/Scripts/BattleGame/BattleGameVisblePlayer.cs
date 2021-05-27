using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;
using Photon.Deterministic;

public unsafe class BattleGameVisblePlayer : QuantumCallbacks
{
    public long      roleid;

    public string   rolename;

    public int head;

    public int avater;

    public int hair;

    public int hairColor;

    public int bodyColor;

    public int      playerid;

    public bool     bIsMatster;

    private bool    bIsInit = false;

    private EntityView entityView = null;

    public void Start()
    {
        entityView = GetComponent<EntityView>();
    }
    
    public override void OnUpdateView(QuantumGame game)
    {
        if(entityView == null || entityView.EntityRef == EntityRef.None)
        {
            return;
        }

        try
        {
            if (bIsInit == false)
            {
                var f = QuantumRunner.Default.Game.Frames.Verified;
                var fields = f.Get<PlayerFields>(entityView.EntityRef);
                playerid = fields.OwnerID;

                var list = f.ResolveList(f.Global->PlayerList);

                for (int i = 0; i < list.Count; i++)
                {
                    if(list[i].Player == playerid)
                    {
                        roleid = list[i].roleid;
                        rolename = list[i].rolename;
                        avater = list[i].avaterId;
                        head = list[i].headid;
                        hair = list[i].hairId;
                        hairColor = list[i].hairColor;
                        bodyColor = list[i].bodyColor;

                        bIsInit = true;
                    }
                }
            }
        }
        catch (System.Exception)
        {
            return;
        }
    }

    public int GetPlayerId()
    {
        return playerid;
    }

    public long GetRoleId()
    {
        return roleid;
    }
    
    public string GetRoleName()
    {
        return rolename;
    }

    public long GetHeadId()
    {
        return head;
    }

    public long GetAvaterId()
    {
        return avater;
    }

    public long GetHairId()
    {
        return hair;
    }

    public long GetHairColor()
    {
        return hairColor;
    }

    public long GetBodyColor()
    {
        return bodyColor;
    }

    public bool IsInit()
    {
        return bIsInit;
    }

    public void UpdateAnimator()
    {
        var customAnimator = GetComponent<CustomQuantumAnimator>();
        if(customAnimator)
        {
            customAnimator.LoadDoneUpdate();
        }
    }
}