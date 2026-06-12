using UnityEngine;
using System.IO;
using System.Collections;
using TMPro;

public class KinectSessionManager : MonoBehaviour
{
    public static KinectSessionManager Instance;

    [Header("UI Elements")]
    public TMP_Text statusText;
    public TMP_Text triggerStatusText; 
    public TMP_InputField usernameInput;
    public TMP_InputField fileNameInput;
    public GameObject loginPanel;
    
    // Internal state variables
    private string currentUsername = "";
    private string currentState = "Unknown";
    private bool isSessionActive = false;
    private bool isCalibrating = false;
    private string filePath;

    // NEW: Centralized Trigger Tracking Variables
    private string activeTriggerName = "";
    private float activeTriggerDuration = 0f;
    private Coroutine loggingCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (triggerStatusText != null) triggerStatusText.text = ""; 
    }

    void Update()
    {
        if (KinectManager.Instance == null || !KinectManager.Instance.IsInitialized())
        {
            statusText.text = "Waiting for Kinect devices...";
            loginPanel.SetActive(false);
            return;
        }

        if (!isSessionActive && !isCalibrating)
        {
            loginPanel.SetActive(true);
            statusText.text = "Kinect Ready. Enter Username and File Name to Start.";
            return;
        }

        long userId = KinectManager.Instance.GetUserIdByIndex(0);
        if (isCalibrating)
        {
            if (userId == 0)
            {
                statusText.text = "Calibrating... Please step in front of the camera.";
            }
            else
            {
                isCalibrating = false;
                isSessionActive = true;
                statusText.text = "Tracking Active: " + currentUsername + "\nSaving to: " + filePath;
                UpdateTriggerStatus(false, "");
            }
            return;
        }

        if (isSessionActive && userId != 0)
        {
            DetectPosture(userId);
        }
    }

    public void StartSession()
    {
        if (string.IsNullOrEmpty(usernameInput.text)) return;
        
        currentUsername = usernameInput.text;
        
        string customFileName = fileNameInput.text;
        if (string.IsNullOrEmpty(customFileName)) 
        {
            customFileName = "DefaultLog"; 
        }
        
        if (!customFileName.EndsWith(".csv")) 
        {
            customFileName += ".csv";
        }

        filePath = Path.Combine(Application.dataPath, customFileName);

        loginPanel.SetActive(false);
        isCalibrating = true; 
        
        // NEW: Added Timestamp to the CSV Header
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Timestamp,Username,State,Trigger_Name,Duration(Seconds)\n");
        }
    }

    private void DetectPosture(long userId)
    {
        KinectManager kinect = KinectManager.Instance;
        
        int hipIndex = (int)KinectInterop.JointType.SpineBase;
        int leftKneeIndex = (int)KinectInterop.JointType.KneeLeft;

        if (kinect.IsJointTracked(userId, hipIndex) && kinect.IsJointTracked(userId, leftKneeIndex))
        {
            Vector3 hipPos = kinect.GetJointPosition(userId, hipIndex);
            Vector3 kneePos = kinect.GetJointPosition(userId, leftKneeIndex);

            float verticalDistance = hipPos.y - kneePos.y;
            currentState = (verticalDistance < 0.25f) ? "Sitting" : "Standing";
        }
    }

    // --- NEW CENTRALIZED LOGGING LOGIC ---

    public void TriggerEntered(string triggerName)
    {
        if (!isSessionActive) return;

        // Set the new active trigger and instantly reset the stopwatch
        activeTriggerName = triggerName;
        activeTriggerDuration = 0f;
        UpdateTriggerStatus(true, activeTriggerName);

        // Stop the old timer if it was running, and start a fresh one
        if (loggingCoroutine != null) StopCoroutine(loggingCoroutine);
        loggingCoroutine = StartCoroutine(LogEverySecond());
    }

    public void TriggerExited(string triggerName)
    {
        // Only stop the timer if we are exiting the CURRENT active trigger
        // (This prevents bugs if your avatar touches two cubes at once)
        if (activeTriggerName == triggerName)
        {
            activeTriggerName = "";
            UpdateTriggerStatus(false, "");
            
            if (loggingCoroutine != null) 
            {
                StopCoroutine(loggingCoroutine);
                loggingCoroutine = null;
            }
        }
    }

    private IEnumerator LogEverySecond()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            activeTriggerDuration += 1f;

            // Generate real-time timestamp
            string currentTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Write to CSV including the timestamp
            string logEntry = $"{currentTime},{currentUsername},{currentState},{activeTriggerName},{activeTriggerDuration.ToString("F1")}\n";
            File.AppendAllText(filePath, logEntry);
        }
    }

    private void UpdateTriggerStatus(bool isInside, string triggerName)
    {
        if (triggerStatusText == null) return;

        if (isInside)
        {
            triggerStatusText.text = $"ZONE ENGAGED: {triggerName}";
            triggerStatusText.color = Color.green;
        }
        else
        {
            triggerStatusText.text = "ZONE: Empty";
            triggerStatusText.color = Color.yellow;
        }
    }
}