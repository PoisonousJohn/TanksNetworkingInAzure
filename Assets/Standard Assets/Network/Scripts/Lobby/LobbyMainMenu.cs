using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

namespace Prototype.NetworkLobby
{
    //Main menu, mainly only a bunch of callback called by the UI (setup throught the Inspector)
    public class LobbyMainMenu : MonoBehaviour
    {
        public LobbyManager lobbyManager;

        public RectTransform lobbyServerList;
        public RectTransform lobbyServerRegistryList;
        public RectTransform lobbyPanel;

        public InputField ipInput;
        public InputField matchNameInput;

        public void OnEnable()
        {

            ipInput.onEndEdit.RemoveAllListeners();
            ipInput.onEndEdit.AddListener(onEndEditIP);

            matchNameInput.onEndEdit.RemoveAllListeners();
            matchNameInput.onEndEdit.AddListener(onEndEditGameName);
            lobbyManager.topPanel.ToggleVisibility(true);
        }

#if DEDICATED_SERVER_MODE
        private void Start()
        {
            OnClickDedicated();
        }
#endif

        public void OnClickHost()
        {
            lobbyManager.StartHost();
        }

        public void OnConnectToServersRegistry()
        {
            lobbyManager.backDelegate = lobbyManager.SimpleBackClbk;
            lobbyManager.ChangeTo(lobbyServerRegistryList);
        }

        public void OnClickJoin()
        {
            lobbyManager.JoinDedicatedServer(ipInput.text, 7777);
        }

        public void OnClickDedicated()
        {
            lobbyManager.ChangeTo(null);
            Network.Disconnect();
            NetworkServer.Shutdown();
            NetworkServer.Reset();
            lobbyManager.ChoosePortAndStart();

            Debug.LogWarning("Server listening on: " + lobbyManager.networkAddress + ":"
                                    + lobbyManager.networkPort);

#if !DEDICATED_SERVER_MODE
            lobbyManager.backDelegate = lobbyManager.StopServerClbk;
#endif
        }

        public void OnClickCreateMatchmakingGame()
        {
            lobbyManager.StartMatchMaker();
            lobbyManager.matchMaker.CreateMatch(
                matchNameInput.text,
                (uint)lobbyManager.maxPlayers,
                true,
				"", "", "", 0, 0,
				lobbyManager.OnMatchCreate);

            lobbyManager.backDelegate = lobbyManager.StopHost;
            lobbyManager._isMatchmaking = true;
            lobbyManager.DisplayIsConnecting();

            lobbyManager.SetServerInfo("Matchmaker Host", lobbyManager.matchHost);
        }

        public void OnClickOpenServerList()
        {
            lobbyManager.StartMatchMaker();
            lobbyManager.backDelegate = lobbyManager.SimpleBackClbk;
            lobbyManager.ChangeTo(lobbyServerList);
        }

        void onEndEditIP(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                OnClickJoin();
            }
        }

        void onEndEditGameName(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                OnClickCreateMatchmakingGame();
            }
        }

    }
}
