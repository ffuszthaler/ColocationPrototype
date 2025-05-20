using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class VRConsoleLogger : MonoBehaviour
{
    [Header("UI References")]
    public GameObject logEntryPrefab; // Should contain a TMP_Text component
    public Transform contentPanel;

    [Header("Settings")]
    public int maxLogs = 100;

    private Queue<GameObject> logEntries = new Queue<GameObject>();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        GameObject logObj = Instantiate(logEntryPrefab, contentPanel);
        TMP_Text logText = logObj.GetComponent<TMP_Text>();

        if (logText == null)
        {
            Debug.LogWarning("Log Entry Prefab is missing TMP_Text component.");
            Destroy(logObj);
            return;
        }

        switch (type)
        {
            case LogType.Warning:
                logText.color = Color.yellow;
                break;
            case LogType.Error:
            case LogType.Exception:
                logText.color = Color.red;
                break;
            default:
                logText.color = Color.white;
                break;
        }

        logText.text = logString;

        logEntries.Enqueue(logObj);

        if (logEntries.Count > maxLogs)
        {
            GameObject oldLog = logEntries.Dequeue();
            Destroy(oldLog);
        }
    }
}
