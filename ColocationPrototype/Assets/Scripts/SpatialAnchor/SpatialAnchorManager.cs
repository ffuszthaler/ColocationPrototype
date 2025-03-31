using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

public class SpatialAnchorManager : MonoBehaviour
{
    private Guid anchorGroupID;
    
    [SerializeField] ShareUUID uuid;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anchorGroupID = Guid.NewGuid();
        StartCoroutine(CreateSpacialAnchor());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator CreateSpacialAnchor()
    {
        Debug.Log("Creating spacial anchor");
        
        var anchor = gameObject.AddComponent<OVRSpatialAnchor>();
        yield return new WaitUntil(() => anchor.Created);
        
        Debug.Log("Created Anchor: " + anchor.Uuid.ToString());

        var saveResult =   anchor.SaveAnchorAsync();
        yield return new WaitUntil(() => saveResult.IsCompleted);
        
        var saveRes = saveResult.GetResult();

        if (saveRes.Success)
        {
            Debug.Log("Saved spacial anchor: " + saveRes.Status.ToString());
        }
        else
        {
            Debug.Log("Failed to save spacial anchor: " + saveRes.Status.ToString());
        }

        var shareResult = anchor.ShareAsync(anchorGroupID);
        yield return new WaitUntil(() => shareResult.IsCompleted);
        
        var shareRes = shareResult.GetResult();
        
        if (shareRes.Success)
        {
            // set network variable to uuid after sharing it with meta
            FixedString64Bytes guid = shareResult.ToString(); 
            uuid.ColocationUUID.Value = guid;
            
            Debug.Log("Shared spacial anchor: " + shareRes.Status.ToString());
        }
        else
        {
            Debug.Log("Failed to share spacial anchor: " + shareRes.Status.ToString());
        }
    }
}
