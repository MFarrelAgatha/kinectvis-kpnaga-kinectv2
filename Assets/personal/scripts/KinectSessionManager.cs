using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using TMPro;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public class SaveFileDialogWrapper
{
    public int structSize = 0;
    public IntPtr dlgOwner = IntPtr.Zero;
    public IntPtr instance = IntPtr.Zero;
    public string filter = null;
    public string customFilter = null;
    public int maxCustFilter = 0;
    public int filterIndex = 0;
    public string file = null;
    public int maxFile = 0;
    public string fileTitle = null;
    public int maxFileTitle = 0;
    public string initialDir = null;
    public string title = null;
    public int flags = 0;
    public short fileOffset = 0;
    public short fileExtension = 0;
    public string defExt = null;
    public IntPtr custData = IntPtr.Zero;
    public IntPtr hook = IntPtr.Zero;
    public string templateName = null;
    public IntPtr reservedPtr = IntPtr.Zero;
    public int reservedInt = 0;
    public int flagsEx = 0;
}

public class KinectSessionManager : MonoBehaviour
{
    public static KinectSessionManager Instance;

    [Header("UI Elements")]
    public TMP_Text statusText;
    public TMP_Text triggerStatusText;
    [Tooltip("Drag a TextMeshPro Text UI element here to see the user's posture state live.")]
    public TMP_Text postureStatusText;
    public TMP_InputField usernameInput;
    public GameObject loginPanel;

    [Header("Custom Algorithm Settings")]
    [Tooltip("Select which specific bone/joint you want to track from the dropdown.")]
    public KinectInterop.JointType trackingJoint = KinectInterop.JointType.SpineBase;

    [Tooltip("Drag your floor plane or floor GameObject here. If empty, it will default to an absolute world Y position of 0.")]
    public Transform floorTransform;

    [Range(0.05f, 1.50f)]
    [Tooltip("Distance threshold (meters) from the selected bone to the floor to trigger the 'Sitting' state.")]
    public float sitThreshold = 0.25f;

    [Header("Menu Effects to Disable on Start")]
    [Tooltip("Drag your Main Camera here (Unity will automatically find the shake script)")]
    public MonoBehaviour cameraShakeComponent;
    [Tooltip("Drag your Main Menu Effects component/script here")]
    public MonoBehaviour mainMenuEffectsComponent;

    [Header("Main Menu Optimization Safeguard")]
    [Tooltip("Drag the GameObject containing your Kinect Avatars, Body Viewers, or Joint meshes here so they stay turned off on the main menu.")]
    public GameObject kinectTrackingVisuals;

    private string currentUsername = "";
    private string currentState = "Unknown";
    private bool isSessionActive = false;
    private bool isCalibrating = false;
    private string filePath = "";

    private string activeTriggerName = "";
    private float activeTriggerDuration = 0f;
    private Coroutine loggingCoroutine;
    private Coroutine shakeCoroutine;

    public string CurrentState => currentState;
    public bool IsSessionActive => isSessionActive;

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetSaveFileName([In, Out] SaveFileDialogWrapper ofn);

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (triggerStatusText != null) triggerStatusText.text = "";
        if (postureStatusText != null) postureStatusText.text = "POSTURE: Waiting for session...";

        // SAFEGUARD: Ensure tracking visualization is turned off on the main menu
        if (kinectTrackingVisuals != null) kinectTrackingVisuals.SetActive(false);
    }

    void Update()
    {
        if (KinectManager.Instance == null || !KinectManager.Instance.IsInitialized())
        {
            statusText.text = "Waiting for Kinect devices...";
            loginPanel.SetActive(false);
            if (postureStatusText != null) postureStatusText.text = "POSTURE: Kinect Offline";
            return;
        }

        // SAFEGUARD: Stop tracking logic processing entirely if we are resting on the login/menu loop
        if (!isSessionActive && !isCalibrating)
        {
            loginPanel.SetActive(true);
            if (statusText.text == "Waiting for Kinect devices..." || statusText.text == "")
            {
                statusText.text = "Kinect Ready. Enter Username to Start.";
            }
            return;
        }

        long userId = KinectManager.Instance.GetUserIdByIndex(0);
        if (isCalibrating)
        {
            if (userId == 0)
            {
                statusText.text = "Calibrating... Please step in front of the camera.";
                if (postureStatusText != null) postureStatusText.text = "POSTURE: User Not Detected";
            }
            else
            {
                isCalibrating = false;
                isSessionActive = true;
                statusText.text = "Tracking Active: " + currentUsername + "\nSaving to: " + Path.GetFileName(filePath);
                UpdateTriggerStatus(false, "");
            }
            return;
        }

        if (isSessionActive && userId != 0)
        {
            DetectPosture(userId);
        }
        else if (isSessionActive && userId == 0)
        {
            if (postureStatusText != null)
            {
                postureStatusText.text = "POSTURE: Tracking Lost";
                postureStatusText.color = Color.red;
            }
        }
    }

    public void StartSession()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(usernameInput.text.Trim()))
        {
            statusText.text = "<color=red>Please fill your name!</color>";

            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeLoginPanel());
            return;
        }

        string chosenPath = "";

#if UNITY_EDITOR
        chosenPath = UnityEditor.EditorUtility.SaveFilePanel(
            "Select Where to Save your CSV Log File",
            Application.dataPath,
            "KinectLogData",
            "csv"
        );

        if (string.IsNullOrEmpty(chosenPath)) return;
        filePath = chosenPath;

#else
        SaveFileDialogWrapper ofn = new SaveFileDialogWrapper();
        ofn.structSize = Marshal.SizeOf(ofn);
        ofn.filter = "CSV Files (*.csv)\0*.csv\0All Files (*.*)\0*.*\0";
        
        ofn.file = new string(new char[512]); 
        ofn.maxFile = ofn.file.Length;
        ofn.fileTitle = new string(new char[128]);
        ofn.maxFileTitle = ofn.fileTitle.Length;
        
        ofn.initialDir = Application.dataPath.Replace('/', '\\'); 
        ofn.title = "Select Where to Save your CSV Log File";
        ofn.defExt = "csv";
        ofn.flags = 0x00080000 | 0x00000800 | 0x00000002; 

        if (GetSaveFileName(ofn))
        {
            filePath = ofn.file;
        }
        else
        {
            return; 
        }
#endif

        currentUsername = usernameInput.text;
        loginPanel.SetActive(false);
        isCalibrating = true;

        if (cameraShakeComponent != null) cameraShakeComponent.enabled = false;
        if (mainMenuEffectsComponent != null) mainMenuEffectsComponent.enabled = false;

        // SAFEGUARD: Awake tracking displays now that registration rules match up
        if (kinectTrackingVisuals != null) kinectTrackingVisuals.SetActive(true);

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Timestamp,Username,State,Trigger_Name,Duration(Seconds)\n");
        }
    }

    private IEnumerator ShakeLoginPanel()
    {
        if (loginPanel == null) yield break;

        RectTransform panelRect = loginPanel.GetComponent<RectTransform>();
        if (panelRect == null) yield break;

        Vector3 originalPos = panelRect.anchoredPosition;
        float duration = 0.5f;
        float magnitude = 12f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float xOffset = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            panelRect.anchoredPosition = new Vector3(originalPos.x + xOffset, originalPos.y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        panelRect.anchoredPosition = originalPos;
    }

    private void DetectPosture(long userId)
    {
        KinectManager kinect = KinectManager.Instance;
        int jointIndex = (int)trackingJoint;

        if (kinect.IsJointTracked(userId, jointIndex))
        {
            Vector3 jointPos = kinect.GetJointPosition(userId, jointIndex);

            float floorY = (floorTransform != null) ? floorTransform.position.y : 0f;
            float verticalDistance = jointPos.y - floorY;

            currentState = (verticalDistance < sitThreshold) ? "Sitting" : "Standing";

            if (postureStatusText != null)
            {
                string floorLabel = (floorTransform != null) ? floorTransform.name : "Y=0 Plane";
                postureStatusText.text = $"STATE: {currentState.ToUpper()}\n" +
                                         $"Tracking: {trackingJoint}\n" +
                                         $"Dist to {floorLabel}: {verticalDistance:F2}m\n" +
                                         $"Threshold Set: {sitThreshold:F2}m";

                postureStatusText.color = (currentState == "Sitting") ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.6f, 0f);
            }
        }
        else
        {
            if (postureStatusText != null)
            {
                postureStatusText.text = $"STATE: Losing Joint Track...\nTargeting: {trackingJoint}";
                postureStatusText.color = Color.gray;
            }
        }
    }

    public void TriggerEntered(string triggerName)
    {
        if (!isSessionActive) return;

        activeTriggerName = triggerName;
        activeTriggerDuration = 0f;
        UpdateTriggerStatus(true, activeTriggerName);

        if (loggingCoroutine != null) StopCoroutine(loggingCoroutine);
        loggingCoroutine = StartCoroutine(LogEverySecond());
    }

    public void TriggerExited(string triggerName)
    {
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

            string currentTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
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