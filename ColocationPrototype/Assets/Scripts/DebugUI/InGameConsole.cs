using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class InGameConsole : MonoBehaviour
{
    public TMP_Text consoleOutputText;
    public ScrollRect scrollRect;
    public int maxLogMessages = 100;

    private List<string> logMessages = new List<string>();
    private bool isConsoleVisible = true;

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
        if (logMessages.Count > maxLogMessages)
        {
            logMessages.RemoveAt(0);
        }
        UpdateConsoleDisplay();
    }

void UpdateConsoleDisplay()
{
    if (consoleOutputText != null)
    {
        // Save current position before update
        float previousScrollPos = scrollRect.verticalNormalizedPosition;

        consoleOutputText.text = string.Join("\n", logMessages);
        Canvas.ForceUpdateCanvases();

        // Only auto-scroll if user was already at bottom
        if (previousScrollPos <= 0.01f)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            // Restore user scroll position
           //scrollRect.verticalNormalizedPosition = previousScrollPos;
        }
    }
}


    public void ClearConsole()
    {
        logMessages.Clear();
        UpdateConsoleDisplay();
    }
}
