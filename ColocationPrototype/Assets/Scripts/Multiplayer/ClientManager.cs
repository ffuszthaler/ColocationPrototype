using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    public class ClientManager : MonoBehaviour
    {
        public static ClientManager Instance { get; private set; }

        [SerializeField] private AlignPlayer alignPlayer; // Reference to the alignment script
        [SerializeField] private GameObject anchorPrefab; // The anchor prefab to use for alignment

        private string _targetAnchorUUID;       // The UUID we should create (as host) or load (as client)
        private OVRSpatialAnchor _alignmentAnchor; // The actual anchor component used for alignment
        private bool _isHost = false;           // Is this client responsible for creating the anchor?
        private bool _anchorLoadRequested = false; // Prevent duplicate load attempts
        private bool _isAligned = false;        // Are we currently aligned to an anchor?

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            // Need to determine if this client is the host AFTER connecting to the network
            // This might happen in NetworkBehaviour.OnNetworkSpawn or via a message from ServerManager
            StartCoroutine(DetermineHostRoleAndAct());
        }

        private IEnumerator DetermineHostRoleAndAct()
        {
            // Wait until connected to the network
            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient);

            // Basic host check: Is this client the one with the lowest ClientId?
            // A more robust system might be needed depending on the NetworkManager setup (e.g., using IsHost)
            ulong myId = NetworkManager.Singleton.LocalClientId;
            _isHost = true; // Assume host initially
            if (NetworkManager.Singleton.ConnectedClientsIds != null)
            {
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (clientId < myId)
                    {
                        _isHost = false;
                        break;
                    }
                }
            }
            // If running as Server+Client (Host mode in Netcode), this client is definitely the host.
            if (NetworkManager.Singleton.IsHost) _isHost = true;

            Debug.Log($"Client {myId}: Determined role. Is Host = {_isHost}");

            if (_isHost)
            {
                // Host creates the anchor if it doesn't have a UUID yet
                if (string.IsNullOrEmpty(_targetAnchorUUID))
                {
                    AttemptCreateAnchor();
                }
            }
            else
            {
                // Client waits for UUID from ServerManager via NetworkVariable callback (OnAnchorUUIDChanged)
                Debug.Log($"Client {myId}: Waiting for anchor UUID from host/server...");
            }
        }

        // Called by ServerManager when the NetworkVariable changes
        public void ReceiveAnchorUUID(string uuid)
        {
            if (_isHost) return; // Host doesn't need to load its own anchor UUID

            if (string.IsNullOrEmpty(uuid))
            {
                Debug.LogWarning("Received an empty or null UUID.");
                return;
            }

            // If we already have a different UUID or are already aligned, handle appropriately
            if (!string.IsNullOrEmpty(_targetAnchorUUID) && _targetAnchorUUID != uuid)
            {
                Debug.LogWarning($"Received new anchor UUID {uuid}, but was already targeting {_targetAnchorUUID}. Resetting state.");
                // Reset alignment state if the anchor changes
                ResetAlignment();
            }


            if (!_isAligned && !_anchorLoadRequested)
            {
                Debug.Log($"Client received UUID to load: {uuid}");
                _targetAnchorUUID = uuid;
                AttemptLoadAnchor();
            }
        }

        private void AttemptCreateAnchor()
        {
            if (_alignmentAnchor != null || !string.IsNullOrEmpty(_targetAnchorUUID))
            {
                Debug.LogWarning("Create anchor called, but anchor already exists or UUID is set.");
                return; // Already created or received UUID
            }
            if (!_isHost) return; // Only host creates

            Debug.Log("Host: Attempting to create and save anchor...");
            StartCoroutine(CreateAndSaveAnchorRoutine());
        }

        private IEnumerator CreateAndSaveAnchorRoutine()
        {
            // Create anchor object and component
            GameObject anchorGO = new GameObject("Host_AlignmentAnchor");
            // Position it slightly in front of the camera when created
            Transform cameraTransform = Camera.main?.transform; // Ensure you have a main camera
            if (cameraTransform != null)
            {
                anchorGO.transform.position = cameraTransform.position + cameraTransform.forward * 1.0f;
                anchorGO.transform.rotation = cameraTransform.rotation;
            }

            var newAnchor = anchorGO.AddComponent<OVRSpatialAnchor>();
            yield return new WaitUntil(() => newAnchor.Created); // Wait for async creation

            if (!newAnchor.Created)
            {
                Debug.LogError("Host: Failed to create OVRSpatialAnchor component.");
                Destroy(anchorGO);
                yield break;
            }
            Debug.Log($"Host: Anchor component created. Initial UUID: {newAnchor.Uuid}");

            var saveTask = newAnchor.SaveAnchorAsync();
            yield return new WaitUntil(() => saveTask.IsCompleted);

            if (saveTask.GetResult())
            {
                _alignmentAnchor = newAnchor; // Store reference
                _targetAnchorUUID = _alignmentAnchor.Uuid.ToString();
                Debug.Log($"Host: Anchor saved successfully to Cloud. Final UUID: {_targetAnchorUUID}");

                // --- Report UUID to Server ---
                ServerManager serverManager = FindFirstObjectByType<ServerManager>(); // Find the server manager instance
                if(serverManager != null)
                {
                    serverManager.RequestSetSharedAnchorUuidServerRpc(_targetAnchorUUID);
                    Debug.Log("Host: Sent anchor UUID to server.");
                } else { Debug.LogError("Host: Could not find ServerManager to report UUID!"); }
                // --------------------------

                // Host aligns itself immediately
                AlignToAnchor();
            }
            else
            {
                Debug.LogError($"Host: Failed to save anchor: {saveTask.GetResult()}");
                Destroy(anchorGO);
            }
        }

        private void AttemptLoadAnchor()
        {
            if (_alignmentAnchor != null || _anchorLoadRequested || string.IsNullOrEmpty(_targetAnchorUUID) || _isHost)
            {
                return; // Already loaded, load in progress, no UUID, or is the host
            }

            if (Guid.TryParse(_targetAnchorUUID, out Guid anchorGuid))
            {
                Debug.Log($"Client: Attempting to load anchor with UUID: {anchorGuid}");
                _anchorLoadRequested = true;
                StartCoroutine(LoadAnchorRoutine(anchorGuid));
            }
            else
            {
                Debug.LogError($"Client: Invalid anchor UUID format: {_targetAnchorUUID}");
            }
        }

        private IEnumerator LoadAnchorRoutine(Guid anchorUuid)
        {
            List<OVRSpatialAnchor.UnboundAnchor> unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
            Debug.Log($"Client: Requesting to load unbound anchor info for UUID: {anchorUuid}");
            var loadTask = OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(anchorUuid, unboundAnchors);
            yield return new WaitUntil(() => loadTask.IsCompleted);

            _anchorLoadRequested = true; // Reset flag

            if (!loadTask.HasResult)
            {
                Debug.LogError($"Client: Failed to load anchor UUID {anchorUuid}. Status: {loadTask.GetResult().Status}");
                _anchorLoadRequested = false;
                ResetAlignment();
                yield break;
            }

            var loadedAnchors = loadTask.GetResult().Value;

            if (loadedAnchors.Count == 0)
            {
                Debug.LogError($"Client: Load query successful, but no anchor found matching UUID: {anchorUuid}");
                _anchorLoadRequested = false;
                ResetAlignment();
                yield break;
            }

            Debug.Log($"Client: Successfully queried for UUID {anchorUuid}. Found {loadedAnchors.Count} anchor(s).");
            var unboundAnchorToLocalize = loadedAnchors[0]; // Assume first is the one
        
            Debug.Log($"Client: Calling LocalizeAsync for unbound anchor {unboundAnchorToLocalize.Uuid}...");
            var localizeTask = unboundAnchorToLocalize.LocalizeAsync();
            yield return new WaitUntil(() => localizeTask.IsCompleted);

            if (!localizeTask.GetResult())
            {
                Debug.LogError($"Client: Failed to localize anchor {unboundAnchorToLocalize.Uuid}.");            _anchorLoadRequested = false;
                _anchorLoadRequested = false;
                ResetAlignment();
                yield break;
            }

            Debug.Log($"Client: Anchor {unboundAnchorToLocalize.Uuid} LocalizeAsync reported success. Now binding the created OVRSpatialAnchor component...");
        
            // Bind localized unbound anchor to spatial anchor in prefab
            var anchor = Instantiate(anchorPrefab).GetComponent<OVRSpatialAnchor>();
            unboundAnchorToLocalize.BindTo(anchor);
            _alignmentAnchor = anchor;
            AlignToAnchor();
        }

        private void AlignToAnchor()
        {
            if (_alignmentAnchor != null && _alignmentAnchor.Localized)
            {
                alignPlayer.SetAlignmentAnchor(_alignmentAnchor.transform);
                _isAligned = true;
                Debug.Log($"Alignment successful using anchor: {_alignmentAnchor.Uuid}");
            }
            else
            {
                Debug.LogWarning("AlignToAnchor called, but anchor is null or not tracked.");
                ResetAlignment();
            }
        }

        private void ResetAlignment()
        {
            Debug.Log("Resetting alignment.");
            alignPlayer.SetAlignmentAnchor(null); // Tell AlignPlayer to clear alignment
            _isAligned = false;
            // Don't necessarily destroy the anchor component here unless you know it's invalid
            // It might become tracked later.
        }

        // --- Synchronization on Resume ---
        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) // Resuming
            {
                Debug.Log("Application Resuming...");
                // Reset state flags to allow re-sync checks
                _isAligned = false;
                _anchorLoadRequested = false;

                // Anchor component might survive pause, but tracking needs re-validation
                StartCoroutine(HandleResumeSyncRoutine());
            }
            else // Pausing
            {
                Debug.Log("Application Pausing...");
            }
        }

        private IEnumerator HandleResumeSyncRoutine()
        {
            // Give the system a moment to re-initialize tracking
            yield return new WaitForSeconds(2.0f); // Adjust as needed

            Debug.Log("Checking anchor status after resume...");

            // 1. Check if the existing anchor reference is still valid and becomes tracked
            if (_alignmentAnchor != null)
            {
                float waitStartTime = Time.time;
                yield return new WaitUntil(() => _alignmentAnchor == null || _alignmentAnchor.Localized || (Time.time - waitStartTime > 10.0f)); // Wait for tracking with timeout

                if (_alignmentAnchor != null && _alignmentAnchor.Localized)
                {
                    Debug.Log("Existing anchor re-acquired tracking after resume. Re-aligning.");
                    AlignToAnchor();
                    yield break; // Success
                }
                else
                {
                    Debug.LogWarning("Existing anchor did not re-acquire tracking after resume.");
                    // Don't destroy it immediately, maybe try loading based on UUID
                    ResetAlignment();
                }
            }

            // 2. If no valid tracked anchor, AND we have a UUID, try loading it again
            if (!_isAligned && !string.IsNullOrEmpty(_targetAnchorUUID))
            {
                Debug.Log("Attempting to load anchor by UUID after resume.");
                // Clean up old anchor object if it exists but failed to track
                if (_alignmentAnchor != null)
                {
                    Destroy(_alignmentAnchor.gameObject);
                    _alignmentAnchor = null;
                }
                AttemptLoadAnchor(); // This will start the LoadAnchorRoutine
            }
            else if (!_isAligned)
            {
                Debug.LogError("Cannot re-sync on resume: No target anchor UUID is known.");
            }
        }

        // --- Optional: Interval Synchronization / Tracking Check ---
        private void Update()
        {
            // Simple check: If we think we are aligned, but the anchor loses tracking
            if (_isAligned && (_alignmentAnchor == null || !_alignmentAnchor.Localized))
            {
                Debug.LogWarning($"Alignment anchor {_targetAnchorUUID} lost tracking!");
                ResetAlignment();
                // Optional: Automatically try to re-load after a delay
                // if (!_anchorLoadRequested) { Invoke(nameof(AttemptLoadAnchor), 5.0f); }
            }
        }
    }
}