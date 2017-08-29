using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System.Collections;
using System;
using LitJson;

namespace Prototype.NetworkLobby
{
    [Serializable]
    public class SimpleEndpoint
    {
        public string ip;
        public int port;
    }
    public class LobbyManager : NetworkLobbyManager
    {
        static short MsgKicked = MsgType.Highest + 1;

        static public LobbyManager s_Singleton;


        [Header("Unity UI Lobby")]
        [Tooltip("Time in second between all players ready & match start")]
        public float prematchCountdown = 5.0f;

        [Space]
        [Header("UI Reference")]
        public LobbyTopPanel topPanel;

        public RectTransform mainMenuPanel;
        public RectTransform lobbyPanel;

        public LobbyInfoPanel infoPanel;

        protected RectTransform currentPanel;

        public Button backButton;

        public Text statusInfo;
        public Text hostInfo;

        //Client numPlayers from NetworkManager is always 0, so we count (throught connect/destroy in LobbyPlayer) the number
        //of players, so that even client know how many player there is.
        [HideInInspector]
        public int _playerNumber = 0;

        //used to disconnect a client properly when exiting the matchmaker
        [HideInInspector]
        public bool _isMatchmaking = false;

        protected bool _disconnectServer = false;

        protected ulong _currentMatchID;

        protected LobbyHook _lobbyHooks;

        void Start()
        {
            s_Singleton = this;
            _lobbyHooks = GetComponent<Prototype.NetworkLobby.LobbyHook>();
            currentPanel = mainMenuPanel;

            backButton.gameObject.SetActive(false);
            GetComponent<Canvas>().enabled = true;

            DontDestroyOnLoad(gameObject);

            SetServerInfo("Offline", "None");
        }

        public void JoinDedicatedServer(string ip, int port)
        {

            ChangeTo(lobbyPanel);

            networkAddress = ip;
            networkPort = port;
            StartClient();

            backDelegate = StopClientClbk;
            DisplayIsConnecting();

            SetServerInfo("Connecting...", networkAddress + ":" + networkPort);
        }

        public void ChoosePortAndStart()
        {
            int startPort = 7777;
            const int maxPortsPerNode = 10;
            networkAddress = "0.0.0.0";
            for (int i = startPort; i < startPort + maxPortsPerNode; ++i)
            {
                networkPort = i;
                Debug.LogWarning("Trying to start server on port " + i);
                if (NetworkManager.singleton.StartServer())
                {
#if DEDICATED_SERVER_MODE
                    StartCoroutine(StartHeartbeat());
#endif
                    return;
                }
                Debug.LogWarning("Port " + i + " already taken");
            }
            Debug.LogError("Failed to find a port. Quitting");
            Application.Quit();
        }

        public override void OnLobbyClientSceneChanged(NetworkConnection conn)
        {
            if (SceneManager.GetSceneAt(0).name == lobbyScene)
            {
                if (topPanel.isInGame)
                {
                    ChangeTo(lobbyPanel);
                    if (_isMatchmaking)
                    {
                        if (conn.playerControllers[0].unetView.isServer)
                        {
                            backDelegate = StopHostClbk;
                        }
                        else
                        {
                            backDelegate = StopClientClbk;
                        }
                    }
                    else
                    {
                        if (conn.playerControllers[0].unetView.isClient)
                        {
                            backDelegate = StopHostClbk;
                        }
                        else
                        {
                            backDelegate = StopClientClbk;
                        }
                    }
                }
                else
                {
                    ChangeTo(mainMenuPanel);
                }

                topPanel.ToggleVisibility(true);
                topPanel.isInGame = false;
            }
            else
            {
                ChangeTo(null);

                Destroy(GameObject.Find("MainMenuUI(Clone)"));

                //backDelegate = StopGameClbk;
                topPanel.isInGame = true;
                topPanel.ToggleVisibility(false);
            }
        }

        public void ChangeTo(RectTransform newPanel)
        {
            if (currentPanel != null)
            {
                currentPanel.gameObject.SetActive(false);
            }

            if (newPanel != null)
            {
                newPanel.gameObject.SetActive(true);
            }

            currentPanel = newPanel;

            if (currentPanel != mainMenuPanel)
            {
                backButton.gameObject.SetActive(true);
            }
            else
            {
                backButton.gameObject.SetActive(false);
                SetServerInfo("Offline", "None");
                _isMatchmaking = false;
            }
        }

        public void DisplayIsConnecting()
        {
            var _this = this;
            infoPanel.Display("Connecting...", "Cancel", () => { _this.backDelegate(); });
        }

        public void SetServerInfo(string status, string host)
        {
            statusInfo.text = status;
            hostInfo.text = host;
        }


        public delegate void BackButtonDelegate();
        public BackButtonDelegate backDelegate;
        public void GoBackButton()
        {
            backDelegate();
        }

        // ----------------- Server management

        public void AddLocalPlayer()
        {
            TryToAddPlayer();
        }

        public void RemovePlayer(LobbyPlayer player)
        {
            player.RemovePlayer();
        }

        public void SimpleBackClbk()
        {
            ChangeTo(mainMenuPanel);
        }

        public void StopHostClbk()
        {
            if (_isMatchmaking)
            {
				matchMaker.DestroyMatch((NetworkID)_currentMatchID, 0, OnDestroyMatch);
				_disconnectServer = true;
            }
            else
            {
                StopHost();
            }


            ChangeTo(mainMenuPanel);
        }

        public void StopClientClbk()
        {
            StopClient();

            if (_isMatchmaking)
            {
                StopMatchMaker();
            }

            ChangeTo(mainMenuPanel);
        }

        public void StopServerClbk()
        {
            StopServer();
            ChangeTo(mainMenuPanel);
        }

        class KickMsg : MessageBase { }
        public void KickPlayer(NetworkConnection conn)
        {
            conn.Send(MsgKicked, new KickMsg());
        }




        public void KickedMessageHandler(NetworkMessage netMsg)
        {
            infoPanel.Display("Kicked by Server", "Close", null);
            netMsg.conn.Disconnect();
        }

        //===================

        public override void OnStartHost()
        {
            base.OnStartHost();

            ChangeTo(lobbyPanel);
            backDelegate = StopHostClbk;
            SetServerInfo("Hosting", networkAddress);
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);
            if (_lobbyHooks != null)
            {
                _lobbyHooks.OnServerDisconnect(conn);
            }

        }

        public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController player)
        {
            base.OnServerRemovePlayer(conn, player);
            Debug.LogError("Client disconnected");
        }

        public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
		{
			base.OnMatchCreate(success, extendedInfo, matchInfo);
            _currentMatchID = (System.UInt64)matchInfo.networkId;
		}

		public override void OnDestroyMatch(bool success, string extendedInfo)
		{
			base.OnDestroyMatch(success, extendedInfo);
			if (_disconnectServer)
            {
                StopMatchMaker();
                StopHost();
            }
        }

        //allow to handle the (+) button to add/remove player
        public void OnPlayersNumberModified(int count)
        {
            _playerNumber += count;

            int localPlayerCount = 0;
            foreach (PlayerController p in ClientScene.localPlayers)
                localPlayerCount += (p == null || p.playerControllerId == -1) ? 0 : 1;

        }

        // ----------------- Server callbacks ------------------

        //we want to disable the button JOIN if we don't have enough player
        //But OnLobbyClientConnect isn't called on hosting player. So we override the lobbyPlayer creation
        public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
        {
            GameObject obj = Instantiate(lobbyPlayerPrefab.gameObject) as GameObject;

            LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer>();
            newPlayer.ToggleJoinButton(numPlayers + 1 >= minPlayers);


            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
                }
            }

            return obj;
        }

        public override void OnLobbyServerPlayerRemoved(NetworkConnection conn, short playerControllerId)
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
                }
            }
        }

        public override void OnLobbyServerDisconnect(NetworkConnection conn)
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers >= minPlayers);
                }
            }

        }

        public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
        {
            //This hook allows you to apply state data from the lobby-player to the game-player
            //just subclass "LobbyHook" and add it to the lobby object.

            if (_lobbyHooks)
                _lobbyHooks.OnLobbyServerSceneLoadedForPlayer(this, lobbyPlayer, gamePlayer);

            return true;
        }

        // --- Countdown management

        public override void OnLobbyServerPlayersReady()
        {
			bool allready = true;
			for(int i = 0; i < lobbySlots.Length; ++i)
			{
				if(lobbySlots[i] != null)
					allready &= lobbySlots[i].readyToBegin;
			}

			if(allready)
				StartCoroutine(ServerCountdownCoroutine());
        }

        public IEnumerator ServerCountdownCoroutine()
        {
            float remainingTime = prematchCountdown;
            int floorTime = Mathf.FloorToInt(remainingTime);

            while (remainingTime > 0)
            {
                yield return null;

                remainingTime -= Time.deltaTime;
                int newFloorTime = Mathf.FloorToInt(remainingTime);

                if (newFloorTime != floorTime)
                {//to avoid flooding the network of message, we only send a notice to client when the number of plain seconds change.
                    floorTime = newFloorTime;

                    for (int i = 0; i < lobbySlots.Length; ++i)
                    {
                        if (lobbySlots[i] != null)
                        {//there is maxPlayer slots, so some could be == null, need to test it before accessing!
                            (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(floorTime);
                        }
                    }
                }
            }

            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                if (lobbySlots[i] != null)
                {
                    (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(0);
                }
            }

            ServerChangeScene(playScene);
        }

        // ----------------- Client callbacks ------------------

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            infoPanel.gameObject.SetActive(false);

            conn.RegisterHandler(MsgKicked, KickedMessageHandler);

            if (!NetworkServer.active)
            {//only to do on pure client (not self hosting client)
                ChangeTo(lobbyPanel);
                backDelegate = StopClientClbk;
                SetServerInfo("Client", networkAddress);
            }
        }


        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            ChangeTo(mainMenuPanel);
        }

        public override void OnClientError(NetworkConnection conn, int errorCode)
        {
            ChangeTo(mainMenuPanel);
            infoPanel.Display("Cient error : " + (errorCode == 6 ? "timeout" : errorCode.ToString()), "Close", null);
        }

        private IEnumerator GetPublicIp(Action<string> publicIpFound)
        {
            // how to get VM metadata
            // https://azure.microsoft.com/en-us/blog/announcing-general-availability-of-azure-instance-metadata-service/


#if DEDICATED_LOCALHOST
            var result = "{ \"compute\": { \"location\": \"westeurope\", \"name\": \"k8s-agent-1C26B5AA-1\", \"offer\": \"UbuntuServer\", \"osType\": \"Linux\", \"platformFaultDomain\": \"1\", \"platformUpdateDomain\": \"1\", \"publisher\": \"Canonical\", \"sku\": \"16.04-LTS\", \"version\": \"16.04.201706191\", \"vmId\": \"3caa15bf-7869-4e60-ac5d-55410b4509e8\", \"vmSize\": \"Standard_DS2\" }, \"network\": { \"interface\": [ { \"ipv4\": { \"ipAddress\": [ { \"privateIpAddress\": \"10.240.0.5\", \"publicIpAddress\": \"127.0.0.1\" } ], \"subnet\": [ { \"address\": \"10.240.0.0\", \"prefix\": \"16\" } ] }, \"ipv6\": { \"ipAddress\": [] }, \"macAddress\": \"000D3A2B7B7B\" } ] } }";
#else
            var req = UnityWebRequest.Get("http://169.254.169.254/metadata/instance?api-version=2017-04-02");
            req.SetRequestHeader("Metadata", "true");
            yield return req.Send();
            if (req.isNetworkError || req.isHttpError)
            {
                Debug.LogError("Failed to get VM's metadata");
                yield break;
            }
            Debug.LogWarningFormat("Metadata response: {0}", req.downloadHandler.text);
            var result = req.downloadHandler.text;
#endif
            var obj = LitJson.JsonMapper.ToObject(result);
            var interfaces = obj["network"]["interface"];
            foreach (var i in interfaces)
            {
                foreach (var addr in ((JsonData)i)["ipv4"]["ipAddress"])
                {
                    var addrObj = (IDictionary)addr;
                    if (addrObj.Contains("publicIpAddress"))
                    {
                        var publicIp = addrObj["publicIpAddress"].ToString();
                        Debug.LogWarningFormat("Public ip found: {0}", publicIp);
                        publicIpFound(publicIp);
                        yield break;
                    }
                }
            }
        }

        private IEnumerator StartHeartbeat()
        {
            // just for now we'll ask our IP only once
            string publicIp = null;
            yield return StartCoroutine(GetPublicIp(ip => publicIp = ip));

            if (string.IsNullOrEmpty(publicIp))
            {
                Debug.LogError("failed to detect public ip");
                yield break;
            }

#if DEDICATED_LOCALHOST
            var serverRegistryUrl = "http://localhost:5000/api/servers";
            var heartbeatPeriod = 3;
            var heartbeatPeriodFound = true;
#else
            var serverRegistryUrl = System.Environment.GetEnvironmentVariable("SERVERS_REGISTRY_URL");
            int heartbeatPeriod;
            var heartbeatPeriodFound = int.TryParse(System.Environment.GetEnvironmentVariable("HEARTBEAT_PERIOD"), out heartbeatPeriod);
#endif
            if (string.IsNullOrEmpty(serverRegistryUrl) || !heartbeatPeriodFound)
            {
                Debug.LogError("servers registry configuration is not valid");
                Application.Quit();
            }
            while (true)
            {
                var data = LitJson.JsonMapper.ToJson(new SimpleEndpoint{
                    ip = publicIp,
                    port = networkPort
                });
                var request = UnityWebRequest.Post(serverRegistryUrl, "");
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(data));
                request.SetRequestHeader("Content-Type", "application/json");
                request.uploadHandler.contentType = "application/json";
                request.Send();
                yield return new WaitForSeconds(heartbeatPeriod);
            }
        }
    }
}
