using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;
using Photon.Realtime;
using Photon.Deterministic;
using ExitGames.Client.Photon;
using Quantum.Collections;

public unsafe class BattleGameService : MonoBehaviour, IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks
{
    public static BattleGameService Instance { get; private set; }

    public QuantumLoadBalancingClient Client { get; set; }

    public QuantumGame Game { get; set; }

    OpJoinRandomRoomParams joinRandomParams = null;
    EnterRoomParams enterRoomParams = null;
    RoomOptions roomOptions = null;

    private Coroutine JoinRoomCoroutine = null;
    private Coroutine LeaveRoomCoroutine = null;

    private RuntimePlayer localPlayerData = new RuntimePlayer();

    private bool needLoadPlayer = false;

    private int needLoadPlayerCount = 0;

    private ArrayList PlayerScoreList = new ArrayList();

    private void Awake()
    {
        Instance = this;
        QuantumRunner.Init();
        Client = new QuantumLoadBalancingClient(PhotonServerSettings.Instance.AppSettings.Protocol);
        Client.AddCallbackTarget(this);

        joinRandomParams = new OpJoinRandomRoomParams();
        joinRandomParams.ExpectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable();

        enterRoomParams = new EnterRoomParams();

        roomOptions = new RoomOptions();
        roomOptions.Plugins = new string[] { "QuantumPlugin" };
        roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable();
        enterRoomParams.RoomOptions = roomOptions;

        QuantumCallback.Subscribe(this, (CallbackGameStarted c) => OnGameStarted(c.Game));
        QuantumCallback.Subscribe(this, (CallbackGameResynced c) => OnGameResync(c.Game));
        QuantumCallback.Subscribe(this, (CallbackGameDestroyed c) => OnGameDestroyed(c.Game));
        QuantumCallback.Subscribe(this, (CallbackUpdateView c) => OnUpdateView(c.Game));
        QuantumCallback.Subscribe(this, (CallbackSimulateFinished c) => OnSimulateFinished(c.Game, c.Frame));
        QuantumCallback.Subscribe(this, (CallbackChecksumError c) => OnChecksumError(c.Game, c.Error, c.Frames));

        QuantumEvent.Subscribe(this, (EventOnGameRoundLoadMap c) => OnGameRoundLoadMapDone(c.Game));
        QuantumEvent.Subscribe(this, (EventOnGameRoundLoadScene c) => OnGameRoundLoadScene(c.Game, c.RoundIndex, c.GameType));
        QuantumEvent.Subscribe(this, (EventOnGameRoundLoadPlayer c) => OnGameRoundLoadPlayer(c.Game, c.NeedLoadCount));
        QuantumEvent.Subscribe(this, (EventOnGameRoundEnter c) => OnGameRoundEnter(c.Game));
        QuantumEvent.Subscribe(this, (EventOnGameRoundReady c) => OnGameRoundReady(c.Game));
        QuantumEvent.Subscribe(this, (EventOnGameRoundRun c) => OnGameRoundRun(c.Game));
        QuantumEvent.Subscribe(this, (EventOnGameRoundPlayerEnd c) => OnGameRoundPlayerEnd(c.Game, c.order, c.player));
        QuantumEvent.Subscribe(this, (EventOnGameRoundFinish c) => OnGameRoundFinish(c.Game));
        QuantumEvent.Subscribe(this, (EventOnGameRoundEnd c) => OnGameRoundEnd(c.Game));
        QuantumEvent.Subscribe(this, (EventOnGameEnd c) => OnGameEnd(c.Game));
        QuantumEvent.Subscribe(this, (EventOnGameEvent c) => OnGameEvent(c.Game, c.eventname, c.Params));

    }


    private void OnDestroy()
    {
        Disconnect();
        Client.RemoveCallbackTarget(this);
        Client = null;
    }


    public void Disconnect()
    {
        if (Client != null)
        {
            Client.Disconnect();
        }
    }
    private void Update()
    {
        if (Client == null)
        {
            return;
        }

        Client.Service();

        if(needLoadPlayer)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag("Player");
            if (objects.Length != needLoadPlayerCount)
                return;
            for(int i = 0; i< objects.Length; i++)
            {
                BattleGameVisblePlayer visble = objects[i].GetComponent<BattleGameVisblePlayer>();
                if (visble.IsInit() == false)
                {
                    return;
                }
            }

            if(PlayerScoreList.Count == 0)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    BattleGameVisblePlayer visble = objects[i].GetComponent<BattleGameVisblePlayer>();
                    BattleGamePlayerSocre socre = new BattleGamePlayerSocre();
                    socre.SetPlayerId(visble.GetPlayerId());
                    socre.SetRoleId(visble.GetRoleId());
                    socre.SetRoleName(visble.GetRoleName());
                    socre.SetHeadId(visble.GetHeadId());
                    socre.SetAvaterId(visble.GetAvaterId());
                    socre.ClearAllScore();
                    PlayerScoreList.Add(socre);
                }
            }


            needLoadPlayer = false;
        }
    }

    public int GetRoomPlayerCount()
    {
        if (Client == null)
        {
            return 0;
        }

        if (Client.InRoom)
        {
            return Client.CurrentRoom.PlayerCount;
        }

        return 0;
    }

    public void ClearPlayerProperties()
    {
        Client.LocalPlayer.CustomProperties.Clear();
    }
    public void SetPlayerProperties(string key, string value)
    {
        if (Client.LocalPlayer.CustomProperties.ContainsKey(key))
        {
            Client.LocalPlayer.CustomProperties[key] = value;
        }
        else
        {
            Client.LocalPlayer.CustomProperties.Add(key, value);
        }
    }

    public void SetPlayerProperties(string key, long value)
    {
        if (Client.LocalPlayer.CustomProperties.ContainsKey(key))
        {
            Client.LocalPlayer.CustomProperties[key] = value;
        }
        else
        {
            Client.LocalPlayer.CustomProperties.Add(key, value);
        }
    }

    public void ClearJoinRandomRoomParams()
    {
        joinRandomParams.ExpectedCustomRoomProperties.Clear();
    }

    public void SetJoinRandomRoomParams(string key, string value)
    {
        if (joinRandomParams.ExpectedCustomRoomProperties.ContainsKey(key))
        {
            joinRandomParams.ExpectedCustomRoomProperties[key] = value;
        }
        else
        {
            joinRandomParams.ExpectedCustomRoomProperties.Add(key, value);
        }
    }

    public void SetRoomOptionsParams(bool bVisible, byte MaxPlayer)
    {
        roomOptions.IsVisible = bVisible;
        roomOptions.MaxPlayers = MaxPlayer;
    }

    public void ClearRoomOptionsCustomParams()
    {
        roomOptions.CustomRoomProperties.Clear();
    }

    public void SetRoomOptionsCustomParams(string key, string value)
    {
        if (roomOptions.CustomRoomProperties.ContainsKey(key))
        {
            roomOptions.CustomRoomProperties[key] = value;
        }
        else
        {
            roomOptions.CustomRoomProperties.Add(key, value);
        }
    }

    public void StartMatching(string mapName, string region)
    {
        JoinRoomCoroutine = StartCoroutine(LoadRoomCoroutine(mapName, region));
    }

    public void StopMatching()
    {
        if (JoinRoomCoroutine != null)
        {
            StopCoroutine(JoinRoomCoroutine);
            JoinRoomCoroutine = null;
        }
    }


    //开始进入房间 等待匹配
    public IEnumerator LoadRoomCoroutine(string mapName, string region)
    {
        PlayerScoreList.Clear();
        var appSettings = PhotonServerSettings.CloneAppSettings(PhotonServerSettings.Instance.AppSettings);
        appSettings.FixedRegion = region;

        if (region == "cn")
        {
            appSettings.Server = "ns.photonengine.cn";
        }

        Client.ConnectUsingSettings(appSettings);

        while (!Client.IsConnectedAndReady)
        {
            yield return null;
        }

        Debug.LogError("LoadRoomCoroutine");
        enterRoomParams.Lobby = TypedLobby.Default;
        enterRoomParams.RoomName = mapName;
        Client.OpJoinRandomOrCreateRoom(joinRandomParams, enterRoomParams);
        JoinRoomCoroutine = null;
    }


    #region IConnectionCallbacks

    public void OnConnected()
    {
        Debug.LogError("OnConnected");
    }

    public void OnConnectedToMaster()
    {
        Debug.LogError("OnConnectedToMaster");
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("OnDisconnected");
       
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) 
    {
    }

    public void OnRegionListReceived(RegionHandler regionHandler)
    {
    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {
        
    }

    #endregion

    #region IMatchmakingCallbacks


    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {

        Debug.LogError("OnFriendListUpdateFunc");
    }

    public void OnCreatedRoom()
    {
        Debug.LogError("OnCreatedRoomFunc");
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("OnCreateRoomFailedFunc");
    }

    public void OnJoinedRoom()
    {
        if(Client.LocalPlayer.IsMasterClient == true )
        {
            Debug.LogError("IsMasterClient :" + Client.LocalPlayer.CustomProperties["roleid"]);
        }


        Debug.LogError("OnJoinedRoomFunc");
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("OnJoinRoomFailedFunc");
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogError("OnJoinRandomFailedFunc");
    }

    public void OnLeftRoom()
    {
        Debug.LogError("OnLeftRoomFunc");
    }
    #endregion

    #region IInRoomCallbacks

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        long    roleid = 0;
        string  UserName = "default";
        int     Avater = 0;
        int     Hair = 0;
        int     Head = 0;
        int     HairColor = 0;
        int     BodyColor = 0;

        if (newPlayer.CustomProperties.ContainsKey("roleid"))
        {
            roleid = (long)newPlayer.CustomProperties["roleid"];
        }

        if (newPlayer.CustomProperties.ContainsKey("rolename"))
        {
            UserName = (string)newPlayer.CustomProperties["rolename"];
        }

        if (newPlayer.CustomProperties.ContainsKey("avater"))
        {
            Avater = (int)newPlayer.CustomProperties["avater"];
        }

        if (newPlayer.CustomProperties.ContainsKey("head"))
        {
            Head = (int)newPlayer.CustomProperties["head"];
        }

        if (newPlayer.CustomProperties.ContainsKey("hair"))
        {
            Hair = (int)newPlayer.CustomProperties["hair"];
        }

        if (newPlayer.CustomProperties.ContainsKey("haircolor"))
        {
            HairColor = (int)newPlayer.CustomProperties["haircolor"];
        }

        if (newPlayer.CustomProperties.ContainsKey("bodycolor"))
        {
            BodyColor = (int)newPlayer.CustomProperties["bodycolor"];
        }

        Debug.LogError("OnPlayerEnteredRoom");
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogError("OnPlayerLeftRoom");
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Debug.LogError("OnRoomPropertiesUpdate");
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        Debug.LogError("OnPlayerPropertiesUpdate");
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.LogError("OnMasterClientSwitched");
    }
    #endregion

    #region QuantumCallback

    public void OnGameStarted(QuantumGame game)
    {
        this.Game = game;

        Debug.LogError("OnGameStarted");
    }

    public void OnGameResync(QuantumGame game)
    {
        Debug.LogError("OnGameResync");
    }
    public void OnGameDestroyed(QuantumGame game)
    {
        Debug.LogError("OnGameDestroyed");
    }
    public void OnUpdateView(QuantumGame game) 
    {
    }

    public void OnSimulateFinished(QuantumGame game, Frame frame) 
    {
    }
    public void OnChecksumError(QuantumGame game, DeterministicTickChecksumError error, Frame[] frames) 
    {
        Debug.LogError("OnChecksumError");
    }
    #endregion

    #region QuantumEvent


    public void OnGameRoundLoadMapDone(QuantumGame Game)
    {

        Debug.LogError("OnGameRoundLoadMapDone");
        SendPlayerDataLoadMap();
    }


    public void OnGameRoundLoadScene(QuantumGame Game, int RoundIndex, int GameType)
    {

        Debug.LogError("OnGameRoundLoadScene");
        StartCoroutine("LoadScene");
    }

    IEnumerator LoadScene()
    {
        AsyncOperation operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("TranslateScene");
       
        while (!operation.isDone)
        {
            yield return null;
            
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("DangerousTurntable_Main");
        SendPlayerDataLoadScene();
    }

    public void OnGameRoundLoadPlayer(QuantumGame Game, int playercount)
    {
        Debug.LogError("OnGameRoundLoadPlayer");
        //needLoadPlayer = true;
        //needLoadPlayerCount = playercount;

        SendPlayerDataLoadPlayer();
    }

    public void OnGameRoundEnter(QuantumGame Game)
    {
        Debug.LogError("OnGameRoundEnter");

        for (int i = 0; i < PlayerScoreList.Count; i++)
        {
            BattleGamePlayerSocre socre = (BattleGamePlayerSocre)PlayerScoreList[i];
            socre.ClearRoundScore();
        }

        SendPlayerDataEnter();

    }

    public void OnGameRoundReady(QuantumGame Game)
    {
        Debug.LogError("OnGameRoundReady");
    }


    public void OnGameRoundRun(QuantumGame Game)
    {
        Debug.LogError("OnGameRoundRun");
    }

    public void OnGameRoundPlayerEnd(QuantumGame Game, int order, PlayerRef player)
    {
        Debug.LogError("OnGameRoundPlayerEnd");
        
    }

    public void OnGameRoundFinish(QuantumGame Game)
    {
        Debug.LogError("OnGameRoundFinish");

        SendPlayerDataFinish();
    }

    public void OnGameRoundEnd(QuantumGame Game)
    {
        Debug.LogError("OnGameRoundEnd");
    }


    public void OnGameEnd(QuantumGame Game)
    {
        Debug.LogError("OnGameEnd");
    }


    public void OnGameEvent(QuantumGame Game, QStringUtf8_64 eventname, Ptr paramlist)
    {
        QListPtr<int> listPtr = new QListPtr<int>(paramlist);
        
        
    }
    
    public void SendPlayerDataLoadMap()
    {
        foreach (var lp in this.Game.GetLocalPlayers())
        {
            localPlayerData.IsLoadMap = true;
            localPlayerData.IsLoadScene = false;
            localPlayerData.IsLoadPlayer = false;
            localPlayerData.IsEnter = false;
            localPlayerData.IsFinish = false;
            localPlayerData.roleid = 0;
            localPlayerData.rolename = "";
            localPlayerData.headid = 0;
            localPlayerData.avaterId = 0;
            localPlayerData.hairId = 0;
            localPlayerData.hairColor = 0;
            localPlayerData.bodyColor = 0;
            if (Client.LocalPlayer.CustomProperties.ContainsKey("roleid"))
            {
                localPlayerData.roleid = (long)Client.LocalPlayer.CustomProperties["roleid"];
            }

            if (Client.LocalPlayer.CustomProperties.ContainsKey("rolename"))
            {
                localPlayerData.rolename = (string)Client.LocalPlayer.CustomProperties["rolename"];
            }

            if (Client.LocalPlayer.CustomProperties.ContainsKey("avater"))
            {
                localPlayerData.avaterId = (int)(long)Client.LocalPlayer.CustomProperties["avater"];
            }

            if (Client.LocalPlayer.CustomProperties.ContainsKey("head"))
            {
                localPlayerData.headid = (int)(long)Client.LocalPlayer.CustomProperties["head"];
            }

            if (Client.LocalPlayer.CustomProperties.ContainsKey("hair"))
            {
                localPlayerData.hairId = (int)(long)Client.LocalPlayer.CustomProperties["hair"];
            }

            if (Client.LocalPlayer.CustomProperties.ContainsKey("haircolor"))
            {
                localPlayerData.hairColor = (int)(long)Client.LocalPlayer.CustomProperties["haircolor"];
            }

            if (Client.LocalPlayer.CustomProperties.ContainsKey("bodycolor"))
            {
                localPlayerData.bodyColor = (int)(long)Client.LocalPlayer.CustomProperties["bodycolor"];
            }

            this.Game.SendPlayerData(lp, localPlayerData);
        }
    }

    public void SendPlayerDataLoadScene()
    {
        foreach (var lp in this.Game.GetLocalPlayers())
        {
            localPlayerData.IsLoadScene = true;
            this.Game.SendPlayerData(lp, localPlayerData);
        }
    }

    public void SendPlayerDataLoadPlayer()
    {
        foreach (var lp in this.Game.GetLocalPlayers())
        {
            localPlayerData.IsLoadPlayer = true;
            this.Game.SendPlayerData(lp, localPlayerData);
        }
    }

    public void SendPlayerDataEnter()
    {
        foreach (var lp in this.Game.GetLocalPlayers())
        {
            localPlayerData.IsEnter = true;
            this.Game.SendPlayerData(lp, localPlayerData);
        }
    }

    public void SendPlayerDataFinish()
    {
        foreach (var lp in this.Game.GetLocalPlayers())
        {
            localPlayerData.IsFinish = true;
            this.Game.SendPlayerData(lp, localPlayerData);
        }
    }
    #endregion

    public List<long> GetRoomPlayerAvaterInfo()
    {
        List<long> list = new List<long>();
        if (Client.InRoom)
        {
            foreach (var kv in Client.CurrentRoom.Players)
            {
                list.Add((long)kv.Value.CustomProperties["avater"]);
            }

        }

        return list;
    }

    public int GetGameState()
    {
        return (int)this.Game.Frames.Verified.Global->CurrentGameRound.State;
    }

    public float GetGameReadyTime()
    {
        return this.Game.Frames.Predicted.Global->CurrentGameRound.ReadyTime.AsFloat;
    }

    public float GetGameTime()
    {
        return this.Game.Frames.Predicted.Global->CurrentGameRound.Timer.AsFloat;
    }


    private class RoundPlayerSocreSortCompare : System.Collections.IComparer
    {
        public int Compare(object x, object y)
        {
            return ((BattleGamePlayerSocre)x).GetRoundSocre() - ((BattleGamePlayerSocre)y).GetRoundSocre();
        }
    }

    public int[] GetRoundPlayers()
    {
        RoundPlayerSocreSortCompare SortCompare = new RoundPlayerSocreSortCompare();
        PlayerScoreList.Sort(SortCompare);
        int[] list = new int[PlayerScoreList.Count];
        for(int i = 0; i < PlayerScoreList.Count; i++)
        {
            list[i] = ((BattleGamePlayerSocre)PlayerScoreList[i]).GetPlayerId();
        }
        return list;
    }

    //匹配人数达标 开始游戏
    public void StartGame()
    {
        RuntimeConfigContainer config = GetComponent<RuntimeConfigContainer>();

        var param = new QuantumRunner.StartParameters
        {
            RuntimeConfig = config.Config,
            DeterministicConfig = DeterministicSessionConfigAsset.Instance.Config,
            ReplayProvider = null,
            GameMode = Photon.Deterministic.DeterministicGameMode.Multiplayer,
            InitialFrame = 0,
            PlayerCount = Client.CurrentRoom.PlayerCount,
            LocalPlayerCount = 1,
            RecordingFlags = RecordingFlags.Default,
            NetworkClient = Client
        };

        long localroleid = (long)Client.LocalPlayer.CustomProperties["roleid"];

        Debug.Log("QuantumRunner.StartGame " + localroleid.ToString());
        QuantumRunner.StartGame(localroleid.ToString(), param);

        if(Client.LocalPlayer.IsMasterClient)
        {
        }
        //ReconnectInformation.Refresh(UIMain.Client, TimeSpan.FromMinutes(1));

    }

    public void LeaveRoom()
    {
        if (Client.InRoom)
        {
            LeaveRoomCoroutine = StartCoroutine(LeaveCoroutine());
        }
        
    }

    private IEnumerator LeaveCoroutine()
    {
        yield return null;
        if (QuantumRunner.ShutdownAll(true))
        {
            Client.OpLeaveRoom(false);

            var startWait = Time.realtimeSinceStartup;
            while (Client.CurrentRoom != null && (Time.realtimeSinceStartup - startWait) < 1.0f)
            {
                yield return null;
            }
            LeaveRoomCoroutine = null;
        }
    }

    private bool TryGetRoomProperty<T>(string key, out T value)
    {
        if (Client.CurrentRoom.CustomProperties.TryGetValue(key, out var v) && v is T)
        {
            value = (T)v;
            return true;
        }
        else
        {
            value = default(T);
            return false;
        }
    }
}
