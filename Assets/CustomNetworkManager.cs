using System;
using Mirror;
using UnityEngine;
public class CustomNetworkManager : NetworkManager
{
    public Action OnClientConnectAction;

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        OnClientConnectAction?.Invoke();
        Debug.Log("Client connected!");
    }
}
