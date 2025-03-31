using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class ClientManager : MonoBehaviour
{
    public static ClientManager Instance { get; private set; }
    private string sessionUUID;
    private List<ulong> clientList = new List<ulong>();
    private OVRSpatialAnchor sharedAnchor;
    private Guid anchorGroupID;

    // private List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();

    // [SerializeField] private ShareUUID uuid;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void ReceiveSessionData(string uuid, ulong[] clientIds)
    {
        sessionUUID = uuid;
        clientList = new List<ulong>(clientIds);

        Debug.Log($"Received Session UUID: {uuid}");
        Debug.Log("Updated Client List:");
        foreach (var clientId in clientList)
        {
            Debug.Log($"Client ID: {clientId}");
        }

        HandleSpatialAnchor();
    }

    private void HandleSpatialAnchor()
    {
        if (clientList.Count == 1)
        {
            StartCoroutine(CreateAndShareAnchor());
        }
        else
        {
            StartCoroutine(DownloadSharedAnchors());
        }
    }

    private IEnumerator CreateAndShareAnchor()
    {
        Debug.Log("Creating spatial anchor");
        anchorGroupID = Guid.NewGuid();

        sharedAnchor = gameObject.AddComponent<OVRSpatialAnchor>();
        yield return new WaitUntil(() => sharedAnchor.Created);

        Debug.Log("Created Anchor: " + sharedAnchor.Uuid.ToString());

        var saveResult = sharedAnchor.SaveAnchorAsync();
        yield return new WaitUntil(() => saveResult.IsCompleted);

        var saveRes = saveResult.GetResult();
        if (saveRes.Success)
        {
            Debug.Log("Saved spatial anchor: " + saveRes.Status.ToString());
        }
        else
        {
            Debug.Log("Failed to save spatial anchor: " + saveRes.Status.ToString());
            yield break;
        }

        var shareResult = sharedAnchor.ShareAsync(anchorGroupID);
        yield return new WaitUntil(() => shareResult.IsCompleted);

        var shareRes = shareResult.GetResult();
        if (shareRes.Success)
        {
            // FixedString64Bytes guid = shareRes.ToString(); 
            // uuid.ColocationUUID.Value = guid;

            // _unboundAnchors.Add(shareRes);

            Debug.Log("Shared spatial anchor: " + shareRes.Status.ToString());
        }
        else
        {
            Debug.Log("Failed to share spatial anchor: " + shareRes.Status.ToString());
        }
    }

    private IEnumerator DownloadSharedAnchors()
    {
        Debug.Log("Downloading shared anchor.");
        
        if (Guid.TryParse(sessionUUID, out Guid anchorUuid))
        {
            List<OVRSpatialAnchor.UnboundAnchor> unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
            var anchorRequest = OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(anchorUuid, unboundAnchors);
            yield return new WaitUntil(() => anchorRequest.IsCompleted);
            foreach (var unboundAnchor in anchorRequest.GetResult().Value)
            {
                var localizeReq = unboundAnchor.LocalizeAsync();
                yield return new WaitUntil(() => localizeReq.IsCompleted);
            }
        }
        else
        {
            Debug.LogError("Invalid session UUID format.");
        }
    }

    // async void LoadAnchorsByUuid(IEnumerable<Guid> uuids)
    // {
    //     // Step 1: Load
    //     var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, _unboundAnchors);
    //
    //     if (result.Success)
    //     {
    //         Debug.Log($"Anchors loaded successfully.");
    //
    //         // Note result.Value is the same as _unboundAnchors passed to LoadUnboundAnchorsAsync
    //         foreach (var unboundAnchor in result.Value)
    //         {
    //             // Step 2: Localize
    //             await unboundAnchor.LocalizeAsync();
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogError($"Load failed with error {result.Status}.");
    //     }
    // }
}