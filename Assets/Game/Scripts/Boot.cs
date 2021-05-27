using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boot : MonoBehaviour
{

    public static Boot Instance { get; private set; }

    public bool StartGame = false;

    private void Awake()
    {
        if (!Debug.isDebugBuild)
            Debug.unityLogger.filterLogType = LogType.Error | LogType.Exception | LogType.Assert;

        // 永久存在的单件.
        Instance = this;
        GameObject.DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(StartGame == false)
        {
            if(BattleGameService.Instance.GetRoomPlayerCount() > 0)
            {
                BattleGameService.Instance.StartGame();
                StartGame = true;
            }
        }
    }

    public void StartClick()
    {
        BattleGameService.Instance.SetPlayerProperties("roleid", 1);
        BattleGameService.Instance.SetPlayerProperties("rolename", "aaaa");
        BattleGameService.Instance.SetPlayerProperties("avater", 1);
        BattleGameService.Instance.SetPlayerProperties("head", 1);
        BattleGameService.Instance.SetPlayerProperties("hair", 1);
        BattleGameService.Instance.SetPlayerProperties("haircolor", 1);
        BattleGameService.Instance.SetPlayerProperties("bodycolor", 1);
        BattleGameService.Instance.StartMatching("TestMap", "cn");
    }
}
