using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class TankLobbyHook : Prototype.NetworkLobby.LobbyHook 
{
    public override void OnLobbyServerSceneLoadedForPlayer(NetworkManager manager, GameObject lobbyPlayer, GameObject gamePlayer)
    {
        if (lobbyPlayer == null)
            return;

		Prototype.NetworkLobby.LobbyPlayer lp = lobbyPlayer.GetComponent<Prototype.NetworkLobby.LobbyPlayer>();

        if(lp != null)
            GameManager.AddTank(gamePlayer, lp.slot, lp.playerColor, lp.nameInput.text, lp.playerControllerId, lp.connectionToClient.connectionId);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        GameObject tank = null;
        TankManager tm = null;
        if (GameManager.m_Tanks != null)
        {
            tm = GameManager.m_Tanks.Find(i => i.m_ConnectionID == conn.connectionId);
            if (tm != null)
                tank = tm.m_Instance;
        }
        if (tank != null && GameManager.s_Instance != null)
            GameManager.s_Instance.RemoveTank(tank);
    }
}
