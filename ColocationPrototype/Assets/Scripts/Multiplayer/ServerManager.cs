using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    public class ServerManager : NetworkBehaviour
    {
        // Use a NetworkVariable to sync the definitive anchor UUID from host to clients
        // Use FixedString since Guid isn't directly supported. Store Guid.ToString().
        private readonly NetworkVariable<FixedString128Bytes> sharedAnchorUuid =
            new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private static readonly Dictionary<ulong, string> ConnectedClients = new Dictionary<ulong, string>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

                // If the server is also a client (Host mode), it might need to report its own connection
                if (IsHost)
                {
                    OnClientConnected(NetworkManager.LocalClientId);
                }
            }
            // Client listens for changes to the UUID
            if (IsClient)
            {
                sharedAnchorUuid.OnValueChanged += OnAnchorUUIDChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            // Clean up callbacks & listeners
            if (NetworkManager != null)
            {
                if (IsServer)
                {
                    NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                    NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                }
                if (IsClient)
                {
                    sharedAnchorUuid.OnValueChanged -= OnAnchorUUIDChanged;
                }
            }
            base.OnNetworkDespawn();
        }


        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            ConnectedClients[clientId] = $"Player_{clientId}";
            Debug.Log($"Server: Client connected {clientId}. Total clients: {ConnectedClients.Count}");
            // The UUID might not be set yet if this is the first client.
            // The host client will set it via ServerRpc later.
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;
            if (ConnectedClients.Remove(clientId))
            {
                Debug.Log($"Server: Client disconnected {clientId}. Remaining clients: {ConnectedClients.Count}");
            }
            // If the host disconnects, need logic to potentially select a new host or end session.
        }

        // RPC for the host client to call ONCE to set the anchor UUID
        [ServerRpc(RequireOwnership = false)] // Allow any client to call this, but check logic inside
        public void RequestSetSharedAnchorUuidServerRpc(string anchorUuidString, ServerRpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;

            // Only allow setting the UUID if it hasn't been set yet,
            // OR potentially implement host migration logic.
            // Let's assume the first client (or lowest ClientId) is the host for simplicity.
            bool isHost = IsClientHost(senderClientId); // Implement IsClientHost logic

            if (isHost && string.IsNullOrEmpty(sharedAnchorUuid.Value.ToString()) && !string.IsNullOrEmpty(anchorUuidString))
            {
                Debug.Log($"Server: Received Anchor UUID '{anchorUuidString}' from host client {senderClientId}. Setting NetworkVariable.");
                sharedAnchorUuid.Value = new FixedString128Bytes(anchorUuidString);
            }
            else if (!isHost)
            {
                Debug.LogWarning($"Server: Client {senderClientId} tried to set UUID but is not host.");
            }
            else if (!string.IsNullOrEmpty(sharedAnchorUuid.Value.ToString()))
            {
                Debug.LogWarning($"Server: Anchor UUID already set to {sharedAnchorUuid.Value}. Ignoring request from {senderClientId}.");
            }
        }

        // Example Host Check (adjust as needed)
        private bool IsClientHost(ulong clientId)
        {
            // Simplistic: lowest client ID is host, or if server is also client
            if (IsHost && clientId == NetworkManager.LocalClientId) return true;

            ulong lowestId = ulong.MaxValue;
            if (ConnectedClients.Count > 0)
            {
                foreach(var id in ConnectedClients.Keys) { if (id < lowestId) lowestId = id; }
                return clientId == lowestId;
            }
            return false; // Should not happen if called by a connected client
        }


        // --- Client-side reaction to UUID change ---
        private void OnAnchorUUIDChanged(FixedString128Bytes previousValue, FixedString128Bytes newValue)
        {
            string newUuid = newValue.ToString();
            if (!string.IsNullOrEmpty(newUuid))
            {
                Debug.Log($"Client {NetworkManager.LocalClientId}: Detected shared anchor UUID update: {newUuid}");
                ClientManager.Instance?.ReceiveAnchorUUID(newUuid);
            }
        }
    }
}