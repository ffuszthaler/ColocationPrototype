using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class OwnershipTransferOnGrab : MonoBehaviour
{
    // private Grabbable grabbable;
    private HandGrabInteractable handGrabInteractable;

    private void Awake()
    {
        // grabbable = GetComponent<Grabbable>();
        // grabbable.WhenSelect += OnGrabbed;
        handGrabInteractable = gameObject.GetComponent<HandGrabInteractable>();
    }

    private void Update()
    {
        var hand = handGrabInteractable.Interactors.FirstOrDefault<HandGrabInteractor>();
        if (hand != null)
        {
            Debug.Log("Connected to hand " + hand.gameObject.tag);
            OnGrabbed();
        }
    }

    private void OnGrabbed()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // Transfer ownership to the client who grabbed the cube
            NetworkObject networkObject = GetComponent<NetworkObject>();
            
            // ulong clientId = grabbable.SelectingInteractor.GetComponent<NetworkObject>().OwnerClientId;
            
            ulong clientId = handGrabInteractable.GetComponent<NetworkObject>().OwnerClientId;
            networkObject.ChangeOwnership(clientId);
        }
    }
}