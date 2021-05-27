using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public unsafe class BattleGamePlayerSocre
{
    private long roleid;

    private string rolename;

    private long avater;

    private long head;

    private int playerid;

    private int AllSocre = 0;

    private int RoundSocre = 0;

    public void SetPlayerId(int playerid)
    {
        this.playerid = playerid;
    }

    public int GetPlayerId()
    {
        return playerid;
    }

    public void SetRoleId(long roleid)
    {
        this.roleid = roleid;
    }

    public long GetRoleId()
    {
        return roleid;
    }

    public void SetRoleName(string rolename)
    {
        this.rolename = rolename;
    }

    public string GetRoleName()
    {
        return rolename;
    }

    public void SetHeadId(long head)
    {
        this.head = head;
    }

    public long GetHeadId()
    {
        return head;
    }


    public void SetAvaterId(long avater)
    {
        this.avater = avater;
    }

    public long GetAvaterId()
    {
        return avater;
    }

    public void ClearAllScore()
    {
        AllSocre = 0;
        RoundSocre = 0;
    }

    public void ClearRoundScore()
    {
        RoundSocre = 0;
    }

    public void SetSocre(int iValue)
    {
        AllSocre += AllSocre;
        RoundSocre = iValue;
    }

    public int GetRoundSocre()
    {
        return RoundSocre;
    }

    public int GetAllSocre()
    {
        return AllSocre;
    }
}