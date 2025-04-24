using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class OwnershipTransferOnGrab : MonoBehaviour
{
    // private Grabbable grabbable;
    [SerializeField] private HandGrabInteractable handGrabInteractable;

    private void Awake()
    {
        // grabbable = GetComponent<Grabbable>();
        // grabbable.WhenSelect += OnGrabbed;
        // handGrabInteractable = gameObject.GetComponent<HandGrabInteractable>();
    }

    private void Update()
    {
        // var hand = handGrabInteractable.Interactors.FirstOrDefault<HandGrabInteractor>();
        // if (hand != null)
        // {
        //     Debug.Log("Connected to hand " + hand.gameObject.tag);
        //     OnGrabbed();
        // }
    }

    public void OnGrabbed()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // Transfer ownership to the client who grabbed the cube
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                Debug.Log("Network Object found: " + networkObject.name);
            }
            else
            {
                Debug.Log("Network Object not found!");
            }
            
            // ulong clientId = grabbable.SelectingInteractor.GetComponent<NetworkObject>().OwnerClientId;
            ulong clientId = networkObject.OwnerClientId;
            if (clientId != null)
            {
                Debug.Log("ClientID found: " + clientId);
            }
            networkObject.ChangeOwnership(clientId);
            
            Debug.Log("Is Owner: " + networkObject.IsOwner);
        }
    }
}