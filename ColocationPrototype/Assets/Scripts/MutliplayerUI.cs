using System;
using System.Text;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;

public class MutliplayerUI : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;

    // [SerializeField] ShareUUID guidManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // networkManager = GetComponent<NetworkManager>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        StatusLabels();

        // if (!networkManager.IsClient && !networkManager.IsServer)
        // {
        //     StartButtons();
        // }
        // else
        // {
        //     StatusLabels();
        //     // GUIDDisplay();
        // }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host")) networkManager.StartHost();
        if (GUILayout.Button("Client")) networkManager.StartClient();
        if (GUILayout.Button("Server")) networkManager.StartServer();
    }

    // void GUIDDisplay()
    // {
    //     if (networkManager.IsServer)
    //     {
    //         // GenerateGUID();
    //         if (GUILayout.Button("Generate/Send GUID")) GenerateGUID();
    //     }
    //     
    //     if (guidManager.ColocationGUID.Value.IsEmpty)
    //     {
    //         return;
    //     }
    //     
    //     GUILayout.Label("GUID: " + guidManager.ColocationGUID.Value);
    // }

    void StatusLabels()
    {
        var mode = networkManager.IsHost ? "Host" : networkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
                        networkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
}