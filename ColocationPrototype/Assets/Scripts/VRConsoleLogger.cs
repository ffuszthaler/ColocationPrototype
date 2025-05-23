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
        private List<string> logMessages = new List<string>();

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
        string formattedLog = "";
        switch (type)
        {
            case LogType.Log:
                formattedLog = $"<color=white>{logString}</color>";
                break;
            case LogType.Warning:
                formattedLog = $"<color=yellow>WARNING: {logString}</color>";
                break;
            case LogType.Error:
                formattedLog = $"<color=red>ERROR: {logString}</color>";
                break;
            case LogType.Exception:
                formattedLog = $"<color=red>EXCEPTION: {logString}\n{stackTrace}</color>";
                break;
            case LogType.Assert:
                formattedLog = $"<color=magenta>ASSERT: {logString}</color>";
                break;
            default:
                formattedLog = logString;
                break;

        }
                  logMessages.Add(formattedLog);
        if (logMessages.Count > maxLogs)
        {
            logMessages.RemoveAt(0);
        }

              
        }
    }

