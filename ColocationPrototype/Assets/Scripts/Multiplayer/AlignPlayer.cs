using System.Collections;
using UnityEngine;

namespace Multiplayer
{
    public class AlignPlayer : MonoBehaviour
    {
        // Keep your existing AlignPlayer script, but add a way to handle null transform
        // when alignment is reset.

        // [SerializeField] Transform player; // Your player rig root
        // [SerializeField] Transform playerHands; // Optional hands offset
        [SerializeField] private Transform playerRoot; // Assign the root of your XR rig here (e.g., OVRCameraRig)

        private Transform m_CurrentAlignmentTarget = null;
        private Coroutine m_AlignCoroutine;

        public void SetAlignmentAnchor(Transform anchorTransform)
        {
            if (m_AlignCoroutine != null)
            {
                StopCoroutine(m_AlignCoroutine);
                m_AlignCoroutine = null;
            }

            m_CurrentAlignmentTarget = anchorTransform; // Can be null

            if (m_CurrentAlignmentTarget != null)
            {
                Debug.Log($"AlignPlayer: New alignment target set: {anchorTransform.name}");
                m_AlignCoroutine = StartCoroutine(RealignRoutine(m_CurrentAlignmentTarget));
            }
            else
            {
                Debug.Log("AlignPlayer: Alignment target cleared.");
                // Optionally reset playerRoot position/rotation to a default state
                if (playerRoot != null)
                {
                    // playerRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // Or keep last known good pose?
                }
            }
        }

        IEnumerator RealignRoutine(Transform anchorTransform)
        {
            if (!playerRoot)
            {
                Debug.LogError("AlignPlayer: Player Root transform not assigned!");
                yield break;
            }

            // Wait a frame to ensure transforms are stable
            yield return null;

            // Calculate the inverse of the anchor's pose
            // This tells us where the world origin should be relative to the anchor
            Vector3 targetWorldPosition = anchorTransform.position;
            Quaternion targetWorldRotation = anchorTransform.rotation;

            // We want the player rig's world origin to align with the anchor's pose.
            // However, the player rig itself might be offset from the tracking origin.
            // The simplest approach is often to have a "WorldRoot" or "NetworkOrigin" GameObject
            // that everything networked is parented to, and align *that* object.

            // --- Simple Alignment: Move Player Rig ---
            // This assumes the Player Rig's origin *is* the intended world origin.
            // Calculate where the Player Rig needs to move so its origin matches the anchor's world pose.
            // This often involves inverting the anchor's pose.

            // Position the player rig such that the anchor's world pose becomes the rig's origin (0,0,0)
            // This calculation might need adjustment based on your specific rig setup.
            // If OVRCameraRig's origin represents tracked space origin:
            playerRoot.position = Vector3.zero - targetWorldPosition; // Move origin opposite to anchor
            playerRoot.rotation = Quaternion.Inverse(targetWorldRotation); // Rotate origin opposite to anchor

            // Alternative: Align a common parent "WorldRoot" object
            // GameObject worldRoot = GetWorldRootObject(); // Find your common parent
            // worldRoot.position = targetWorldPosition;
            // worldRoot.rotation = targetWorldRotation;
            // Ensure playerRoot is parented under worldRoot and positioned/rotated correctly relative to it.

            Debug.Log($"AlignPlayer: Realigned Player Root to match anchor {anchorTransform.name}.");

            m_AlignCoroutine = null;
        }
    }
}