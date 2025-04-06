using System.Collections;
using UnityEngine;

public class AlignPlayer : MonoBehaviour
{
    public static AlignPlayer Instance { get; private set; }


    [SerializeField] Transform player;
    [SerializeField] Transform playerHands;


    // SharedAnchor m_CurrentAlignmentAnchor;
    Coroutine m_AlignCoroutine;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public void SetAlignmentAnchor(Transform tf)
    {
        if (m_AlignCoroutine != null)
        {
            StopCoroutine(m_AlignCoroutine);
            m_AlignCoroutine = null;
        }


        // if (m_CurrentAlignmentAnchor)
        // {
        //     m_CurrentAlignmentAnchor.IsSelectedForAlign = false;
        // }
        //
        // m_CurrentAlignmentAnchor = null;

        if (player)
        {
            player.SetPositionAndRotation(default, Quaternion.identity);
        }

        if (!tf || !player)
            return;

        m_AlignCoroutine = StartCoroutine(RealignRoutine(tf));
    }

    IEnumerator RealignRoutine(Transform tf)
    {
        yield return null;

        var anchorTransform = tf.transform;

        player.position = anchorTransform.InverseTransformPoint(Vector3.zero);
        player.eulerAngles = new Vector3(0, -anchorTransform.eulerAngles.y, 0);

        if (playerHands)
        {
            playerHands.SetLocalPositionAndRotation(
                -player.position,
                Quaternion.Inverse(player.rotation)
            );
        }

        // m_CurrentAlignmentAnchor = anchor;
        // anchor.IsSelectedForAlign = true;

        m_AlignCoroutine = null;
    }
}