using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ServerManager : NetworkBehaviour
{
    private static Dictionary<ulong, string> connectedClients = new Dictionary<ulong, string>();
    private string sessionUUID;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            sessionUUID = Guid.NewGuid().ToString();
            
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        connectedClients[clientId] = $"Player_{clientId}";
        SendSessionDataToAll();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        connectedClients.Remove(clientId);
        SendSessionDataToAll();
    }

    private void SendSessionDataToAll()
    {
        List<ulong> clientIds = new List<ulong>(connectedClients.Keys);
        SendSessionDataClientRpc(sessionUUID, clientIds.ToArray());
    }

    [ClientRpc]
    private void SendSessionDataClientRpc(string uuid, ulong[] clientIds)
    {
        ClientManager.Instance.ReceiveSessionData(uuid, clientIds);
    }
}
