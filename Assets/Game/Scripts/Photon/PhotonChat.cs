using Photon.Chat;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhotonChat : MonoBehaviour, IChatClientListener
{
    public static PhotonChat Instance { get; private set; }

    private ChatClient chatClient;
    private AuthenticationValues authenticationValues = new AuthenticationValues();
    private  Dictionary<string, System.Object> authenticationDataDic = new Dictionary<string, System.Object>();
      


    //设置聊天频道
    public void SetPublicChannel(string channel, int messagesFromHistory)
    {
        this.chatClient.Subscribe(channel, messagesFromHistory);
    }

    //设置聊天频道
    public void CancelPublicChannel(string[] channel)
    {
        this.chatClient.Unsubscribe(channel);
    }

    public void SetAuthenticationName(string userId)
    {
        this.authenticationValues.UserId = userId;
    }

    public void SetAuthenticationValues(string key, string value)
    {
        authenticationDataDic[key] = value;
    }

    //链接聊天服务器
    public void Connect(string AppId, string appVersion, string chatRegion)
    {
        this.authenticationValues.SetAuthPostData(authenticationDataDic);

        this.chatClient = new ChatClient(this);
        if(chatRegion == "CN")
        {
            this.chatClient.chatPeer.NameServerHost = "ns.photonengine.cn";
        }
        else
        {
            this.chatClient.chatPeer.NameServerHost = "ns.exitgames.com";
        }
#if UNITY_WEBGL
        this.chatClient.UseBackgroundWorkerForSending = false;
#else
        this.chatClient.UseBackgroundWorkerForSending = true;
#endif
        this.chatClient.ChatRegion = chatRegion;// Set your favourite region. "EU", "US", and "ASIA" are currently supported.
        this.chatClient.Connect(AppId, appVersion, this.authenticationValues);
 
    }

    public void Awake()
    {
        Instance = this;
    }

    //更新持续活动消息
    private void Update()
    {
        if (this.chatClient != null)
        {
            this.chatClient.Service();  
        } 
    }
    public void OnDestroy()
    {
        //Debug.LogError("PhotonChat - OnDestroy：");
        if (this.chatClient != null)
        {
            this.chatClient.Disconnect();
        }
    }
    public void OnApplicationQuit()
    {
        //Debug.LogError("PhotonChat - OnApplicationQuit：");
        if (this.chatClient != null)
        {
            this.chatClient.Disconnect();
        }
    }
    //发送公共频道信息
    public void SendChatPublishMessage(string channelName, string message)
    {
        this.SetAuthenticationValues("chat_message", message);
        this.chatClient.PublishMessage(channelName, this.authenticationDataDic);
    }
    //发送私人信息
    public void SendChatPrivateMessage(string target, object message)
    {
        this.chatClient.SendPrivateMessage(target, message);
    }
    public void OnUserUnsubscribed(string channel, string user)
    {
    }
    public void OnUserSubscribed(string channel, string user)
    {
    }
    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    { 
         
    }
    public void OnUnsubscribed(string[] channels)
    {
    }
    public void OnSubscribed(string[] channels, bool[] results)
    {
         
    }
    public void OnPrivateMessage(string sender, object message, string channelName)
    {
         
    }
    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
      
    }
    public void OnChatStateChange(ChatState state)
    { 
    }
    public void OnConnected()
    {
       
    }
    public void OnDisconnected()
    {
       
    }

    public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
    {
        if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
        {
            UnityEngine.Debug.LogError(message);
        }
        else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
        {
            UnityEngine.Debug.LogWarning(message);
        }
        else
        {
            UnityEngine.Debug.Log(message);
        }
    }
    public void OpenDashboard()
    {
        Application.OpenURL("https://www.photonengine.com/en/Dashboard/Chat");
    }

}