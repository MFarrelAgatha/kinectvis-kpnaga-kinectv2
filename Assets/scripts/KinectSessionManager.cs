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
    [Tooltip("Drag a TextMeshPro Text UI element here to display the active/loaded account name.")]
    public TMP_Text loadedAccountText;
    [Tooltip("Drag a TextMeshPro Text UI element here to display the profile name (misal: 'Nama Profile' di Panel Kiri Atas).")]
    public TMP_Text profileNameText;
    [Tooltip("Drag a TextMeshPro Text UI element here to display the account status.")]
    public TMP_Text accountStatusText;
    public TMP_InputField usernameInput;
    public GameObject loginPanel;

    public string CurrentUsername => currentUsername;

    [Header("Custom Algorithm Settings")]
    [Tooltip("Select which specific bone/joint you want to track from the dropdown.")]
    public KinectInterop.JointType trackingJoint = KinectInterop.JointType.SpineBase;

    [Tooltip("Drag your floor plane or floor GameObject here. If empty, it will default to an absolute world Y position of 0.")]
    public Transform floorTransform;

    [Range(0.05f, 1.50f)]
    [Tooltip("Distance threshold (meters) from the selected bone to the floor to trigger the 'Sitting' state.")]
    public float sitThreshold = 0.25f;

    [Header("Lokasi & Jarak ke Tungku (Multi-Units & XYZ)")]
    [Tooltip("Transform objek Tungku di scene. Jika kosong, otomatis mencari GameObject 'Tungku'.")]
    public Transform tungkuTransform;

    [Tooltip("Transform avatar player di scene. Jika kosong, otomatis mengambil dari PlayerTungkuTracker / scene.")]
    public Transform playerTransform;

    [Tooltip("Rasio konversi 1 meter ke Studs. Default: 3.5714 studs/meter (standard Roblox: 1 stud = 0.28m).")]
    public float studsPerMeter = 3.5714f;

    [Tooltip("UI TextMeshPro opsional untuk menampilkan info jarak dan koordinat Tungku secara realtime.")]
    public TMP_Text tungkuLocationText;

    [Header("Live Data Jarak ke Tungku (Multi-Units)")]
    [Tooltip("Jarak total Euclidean dalam Meter (m).")]
    public float distanceMeters = 0f;

    [Tooltip("Jarak total dalam Studs (studs).")]
    public float distanceStuds = 0f;

    [Tooltip("Jarak total dalam Centimeter (cm).")]
    public float distanceCentimeters = 0f;

    [Tooltip("Jarak total dalam Feet (ft).")]
    public float distanceFeet = 0f;

    [Tooltip("Jarak total dalam Inches (in).")]
    public float distanceInches = 0f;

    [Header("Live Data Selisih Koordinat (Delta XYZ)")]
    [Tooltip("Selisih vektor posisi Player dikurangi Tungku dalam Meter.")]
    public Vector3 deltaXYZ_Meters = Vector3.zero;

    [Tooltip("Selisih vektor posisi dalam Studs.")]
    public Vector3 deltaXYZ_Studs = Vector3.zero;

    public float DeltaX_Meters => deltaXYZ_Meters.x;
    public float DeltaY_Meters => deltaXYZ_Meters.y;
    public float DeltaZ_Meters => deltaXYZ_Meters.z;
    public float DeltaX_Studs => deltaXYZ_Studs.x;
    public float DeltaY_Studs => deltaXYZ_Studs.y;
    public float DeltaZ_Studs => deltaXYZ_Studs.z;

    [Header("Live Data Posisi World (Player & Tungku XYZ)")]
    [Tooltip("Posisi Player di World Space (X, Y, Z).")]
    public Vector3 playerWorldPos = Vector3.zero;

    [Tooltip("Posisi Tungku di World Space (X, Y, Z).")]
    public Vector3 tungkuWorldPos = Vector3.zero;

    [Header("Formatted Text Summary")]
    [Tooltip("String terformat jarak ke tungku untuk tampilan UI/debug.")]
    public string distanceFormatted = "-";

    [Tooltip("String terformat selisih XYZ ke tungku untuk tampilan UI/debug.")]
    public string deltaXYZFormatted = "-";

    // Public properties
    public float DistanceMeters => distanceMeters;
    public float DistanceStuds => distanceStuds;
    public float DistanceCm => distanceCentimeters;
    public float DistanceFeet => distanceFeet;
    public float DistanceInches => distanceInches;
    public Vector3 DeltaXYZ_Meters => deltaXYZ_Meters;
    public Vector3 DeltaXYZ_Studs => deltaXYZ_Studs;
    public Vector3 PlayerWorldPos => playerWorldPos;
    public Vector3 TungkuWorldPos => tungkuWorldPos;

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

    public string CurrentState
    {
        get => currentState;
        set => currentState = value;
    }

    /// <summary>
    /// Memperbarui state postur (Sitting / Standing) secara langsung, disinkronkan dari PlayerTungkuTracker jika diinginkan.
    /// </summary>
    public void SetPostureState(string newState, string detailInfo = "")
    {
        currentState = newState;
        if (postureStatusText != null)
        {
            if (string.IsNullOrEmpty(detailInfo))
            {
                postureStatusText.text = $"STATE: {currentState.ToUpper()}";
            }
            else
            {
                postureStatusText.text = $"STATE: {currentState.ToUpper()}\n{detailInfo}";
            }
            postureStatusText.color = (currentState == "Sitting") ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.6f, 0f);
        }
    }

    public bool IsSessionActive => isSessionActive;

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetSaveFileName([In, Out] SaveFileDialogWrapper ofn);

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName([In, Out] SaveFileDialogWrapper ofn);

    void Awake()
    {
        if (Instance == null) Instance = this;
        AutoFindProfileTexts();
    }

    void Start()
    {
        AutoFindProfileTexts();
        UpdateProfileAccountUI();

        if (triggerStatusText != null) triggerStatusText.text = "";
        if (postureStatusText != null) postureStatusText.text = "POSTURE: Waiting for session...";
        if (accountStatusText != null) accountStatusText.text = "Status: Idle";

        // SAFEGUARD: Ensure tracking visualization is turned off on the main menu
        if (kinectTrackingVisuals != null) kinectTrackingVisuals.SetActive(false);
    }

    public void AutoFindProfileTexts()
    {
        // Jika profileNameText belum di-assign, atau accountStatusText salah mengarah ke teks nama profile
        if (profileNameText == null && accountStatusText != null)
        {
            if (accountStatusText.text.IndexOf("Profile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                accountStatusText.gameObject.name.IndexOf("Profile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                accountStatusText.fontSize > 20f)
            {
                profileNameText = accountStatusText;
                accountStatusText = null;
            }
        }

        if (profileNameText == null)
        {
            GameObject panelKiriAtas = GameObject.Find("Panel Kiri Atas");
            if (panelKiriAtas != null)
            {
                TMP_Text[] texts = panelKiriAtas.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in texts)
                {
                    if (t == loadedAccountText || t == statusText || t.GetComponentInParent<UnityEngine.UI.Button>() != null) continue;
                    string n = t.gameObject.name.ToLower();
                    string txt = t.text.ToLower();
                    if (n.Contains("profile") || txt.Contains("profile") || txt.Contains("nama") || t.fontSize > 20f)
                    {
                        profileNameText = t;
                        break;
                    }
                }
            }
        }
    }

    public void UpdateProfileAccountUI()
    {
        AutoFindProfileTexts();

        string displayName = !string.IsNullOrEmpty(currentUsername) ? currentUsername : "-";

        if (profileNameText != null)
        {
            profileNameText.text = displayName;
        }

        if (loadedAccountText != null)
        {
            loadedAccountText.text = !string.IsNullOrEmpty(currentUsername) ? "Account: " + currentUsername : "Account: -";
        }

        // Cari juga secara dinamis di Panel Kiri Atas agar semua teks profile/account sinkron
        GameObject panelKiriAtas = GameObject.Find("Panel Kiri Atas");
        if (panelKiriAtas != null)
        {
            TMP_Text[] allTexts = panelKiriAtas.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in allTexts)
            {
                if (t == null) continue;
                if (t == profileNameText || t == loadedAccountText) continue;
                if (t == accountStatusText || t == statusText || t.GetComponentInParent<UnityEngine.UI.Button>() != null) continue;

                if (t.text.IndexOf("Nama Profile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.text.IndexOf("Profile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.text.IndexOf("Account:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    t.text = displayName;
                }
            }
        }

        Debug.Log($"<color=#00FFAA>[KinectSessionManager]</color> Profile account UI diupdate ke: <b>{displayName}</b>");
    }

    void Update()
    {
        if (KinectManager.Instance == null || !KinectManager.Instance.IsInitialized())
        {
            statusText.text = "Waiting for Kinect devices...";
            loginPanel.SetActive(false);
            if (postureStatusText != null) postureStatusText.text = "POSTURE: Kinect Offline";
            if (accountStatusText != null) accountStatusText.text = "Status: Kinect Offline";
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
                if (accountStatusText != null) accountStatusText.text = "Status: Calibrating (Waiting for User)";
            }
            else
            {
                isCalibrating = false;
                isSessionActive = true;
                statusText.text = "Tracking Active: " + currentUsername + "\nSaving to: " + Path.GetFileName(filePath);
                if (accountStatusText != null) accountStatusText.text = "Status: Active Tracking";
                UpdateTriggerStatus(false, "");
            }
            return;
        }

        if (isSessionActive && userId != 0)
        {
            DetectPosture(userId);
            if (accountStatusText != null && accountStatusText.text == "Status: Tracking Lost")
            {
                accountStatusText.text = "Status: Active Tracking";
            }
        }
        else if (isSessionActive && userId == 0)
        {
            if (postureStatusText != null)
            {
                postureStatusText.text = "POSTURE: Tracking Lost";
                postureStatusText.color = Color.red;
            }
            if (accountStatusText != null)
            {
                accountStatusText.text = "Status: Tracking Lost";
            }
        }

        // Hitung jarak multi-unit dan koordinat XYZ ke Tungku secara realtime
        UpdateTungkuLocation();
    }

    /// <summary>
    /// Menghitung jarak ke Tungku dalam Meter, Studs, CM, Feet, Inches, serta selisih Delta XYZ dan koordinat World.
    /// </summary>
    public void UpdateTungkuLocation()
    {
        // 1. Cari referensi Tungku dan Player secara otomatis jika kosong
        if (tungkuTransform == null)
        {
            if (PlayerTungkuTracker.Instance != null && PlayerTungkuTracker.Instance.tungkuTransform != null)
            {
                tungkuTransform = PlayerTungkuTracker.Instance.tungkuTransform;
            }
            else
            {
                GameObject tungkuObj = GameObject.Find("Tungku");
                if (tungkuObj != null) tungkuTransform = tungkuObj.transform;
            }
        }

        if (playerTransform == null)
        {
            if (PlayerTungkuTracker.Instance != null && PlayerTungkuTracker.Instance.playerTransform != null)
            {
                playerTransform = PlayerTungkuTracker.Instance.playerTransform;
            }
            else
            {
                AvatarController ac = FindAnyObjectByType<AvatarController>();
                if (ac != null) playerTransform = ac.transform;
                else
                {
                    GameObject pObj = GameObject.FindWithTag("Player");
                    if (pObj != null) playerTransform = pObj.transform;
                }
            }
        }

        if (tungkuTransform == null || playerTransform == null)
        {
            distanceMeters = 0f;
            distanceStuds = 0f;
            distanceCentimeters = 0f;
            distanceFeet = 0f;
            distanceInches = 0f;
            deltaXYZ_Meters = Vector3.zero;
            deltaXYZ_Studs = Vector3.zero;
            distanceFormatted = "-";
            deltaXYZFormatted = "-";
            return;
        }

        // 2. Ambil koordinat world space
        playerWorldPos = playerTransform.position;
        tungkuWorldPos = tungkuTransform.position;

        // 3. Hitung selisih koordinat (Delta XYZ)
        deltaXYZ_Meters = playerWorldPos - tungkuWorldPos;
        float studRatio = studsPerMeter > 0.001f ? studsPerMeter : 3.5714f;
        deltaXYZ_Studs = deltaXYZ_Meters * studRatio;

        // 4. Hitung jarak langsung (Euclidean Distance) dalam multi-satuan
        distanceMeters = Vector3.Distance(playerWorldPos, tungkuWorldPos);
        distanceStuds = distanceMeters * studRatio;
        distanceCentimeters = distanceMeters * 100f;
        distanceFeet = distanceMeters * 3.28084f;
        distanceInches = distanceFeet * 12f;

        // 5. Format string rangkuman (m, studs, cm, ft, inches, dan XYZ)
        distanceFormatted = $"{distanceMeters:F2} m | {distanceStuds:F2} studs ({distanceCentimeters:F0} cm | {distanceFeet:F2} ft)";

        string signX = deltaXYZ_Meters.x >= 0 ? "+" : "";
        string signY = deltaXYZ_Meters.y >= 0 ? "+" : "";
        string signZ = deltaXYZ_Meters.z >= 0 ? "+" : "";
        deltaXYZFormatted = $"X: {signX}{deltaXYZ_Meters.x:F2}m ({signX}{deltaXYZ_Studs.x:F2} st) | " +
                             $"Y: {signY}{deltaXYZ_Meters.y:F2}m ({signY}{deltaXYZ_Studs.y:F2} st) | " +
                             $"Z: {signZ}{deltaXYZ_Meters.z:F2}m ({signZ}{deltaXYZ_Studs.z:F2} st)";

        // 6. Update UI Text opsional jika ditugaskan
        if (tungkuLocationText != null)
        {
            tungkuLocationText.text = $"<b>JARAK TUNGKU:</b> {distanceFormatted}\n<b>SELISIH XYZ:</b> {deltaXYZFormatted}";
        }
    }

    public string GetSaveFolder()
    {
        string saveFolder = Path.Combine(Application.dataPath, "save");
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }
        return saveFolder;
    }

    private void ActivateSession()
    {
        loginPanel.SetActive(false);
        isCalibrating = true;

        if (cameraShakeComponent != null) cameraShakeComponent.enabled = false;
        if (mainMenuEffectsComponent != null) mainMenuEffectsComponent.enabled = false;

        // SAFEGUARD: Awake tracking displays now that registration rules match up
        if (kinectTrackingVisuals != null) kinectTrackingVisuals.SetActive(true);
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

        string saveFolder = GetSaveFolder();
        string chosenPath = "";
        string defaultFileName = $"{usernameInput.text.Trim()}_Log";

#if UNITY_EDITOR
        chosenPath = UnityEditor.EditorUtility.SaveFilePanel(
            "Select Where to Save your CSV Log File",
            saveFolder,
            defaultFileName,
            "csv"
        );

        if (string.IsNullOrEmpty(chosenPath)) return;
        filePath = chosenPath;

#else
        SaveFileDialogWrapper ofn = new SaveFileDialogWrapper();
        ofn.structSize = Marshal.SizeOf(ofn);
        ofn.filter = "CSV Files (*.csv)\0*.csv\0All Files (*.*)\0*.*\0";
        
        string initialFile = defaultFileName + ".csv";
        char[] fileChars = new char[512];
        initialFile.CopyTo(0, fileChars, 0, Mathf.Min(initialFile.Length, fileChars.Length - 1));
        ofn.file = new string(fileChars);
        ofn.maxFile = ofn.file.Length;
        ofn.fileTitle = new string(new char[128]);
        ofn.maxFileTitle = ofn.fileTitle.Length;
        
        ofn.initialDir = saveFolder.Replace('/', '\\'); 
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

        currentUsername = usernameInput.text.Trim();
        UpdateProfileAccountUI();
        if (accountStatusText != null) accountStatusText.text = "Status: New Account Created";
        ActivateSession();

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Timestamp,Username,State,Trigger_Name,Duration(Seconds)\n");
        }
    }

    public void LoadSession()
    {
        string saveFolder = GetSaveFolder();
        string chosenPath = "";

#if UNITY_EDITOR
        chosenPath = UnityEditor.EditorUtility.OpenFilePanel(
            "Select Existing CSV Log File to Load",
            saveFolder,
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
        
        ofn.initialDir = saveFolder.Replace('/', '\\'); 
        ofn.title = "Select Existing CSV Log File to Load";
        ofn.defExt = "csv";
        ofn.flags = 0x00080000 | 0x00001000 | 0x00000800; 

        if (GetOpenFileName(ofn))
        {
            filePath = ofn.file;
        }
        else
        {
            return; 
        }
#endif

        if (!File.Exists(filePath))
        {
            statusText.text = "<color=red>File not found!</color>";
            return;
        }

        // Parse username from existing CSV if input field is empty
        string loadedUsername = "";
        try
        {
            string[] lines = File.ReadAllLines(filePath);
            for (int i = lines.Length - 1; i >= 1; i--)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                string[] cols = line.Split(',');
                if (cols.Length >= 2)
                {
                    if (cols[0].Contains("-") || cols[0].Contains(":"))
                    {
                        loadedUsername = cols[1].Trim();
                    }
                    else
                    {
                        loadedUsername = cols[0].Trim();
                    }
                    if (!string.IsNullOrEmpty(loadedUsername)) break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KinectSessionManager] Could not read username from CSV: " + ex.Message);
        }

        // Prioritaskan nama akun dari isi file CSV yang dipilih, bukan input field lama
        if (!string.IsNullOrEmpty(loadedUsername))
        {
            currentUsername = loadedUsername;
        }
        else
        {
            string cleanFn = Path.GetFileNameWithoutExtension(filePath);
            if (cleanFn.EndsWith("_Log", StringComparison.OrdinalIgnoreCase))
            {
                cleanFn = cleanFn.Substring(0, cleanFn.Length - 4);
            }
            currentUsername = cleanFn;
        }

        if (usernameInput != null)
        {
            usernameInput.text = currentUsername;
        }

        // Ensure header exists if file was empty
        if (new FileInfo(filePath).Length == 0)
        {
            File.WriteAllText(filePath, "Timestamp,Username,State,Trigger_Name,Duration(Seconds),Dist_Meters,Dist_Studs,Dist_Cm,Dist_Feet,Delta_X_m,Delta_Y_m,Delta_Z_m,Delta_X_studs,Delta_Y_studs,Delta_Z_studs,Player_X,Player_Y,Player_Z,Tungku_X,Tungku_Y,Tungku_Z\n");
        }

        UpdateProfileAccountUI();
        if (accountStatusText != null) accountStatusText.text = "Status: Loaded from " + Path.GetFileName(filePath);

        ActivateSession();
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
        // Jika PlayerTungkuTracker aktif, sinkronkan langsung status postur fisik avatar
        if (PlayerTungkuTracker.Instance != null)
        {
            currentState = PlayerTungkuTracker.Instance.Posture;
            if (postureStatusText != null)
            {
                bool isSittingOrSquatting = (currentState != "Standing" && currentState != "Unknown");
                postureStatusText.text = $"STATE: {currentState.ToUpper()} (Tracker Synced)\n" +
                                         $"Hip Height: {PlayerTungkuTracker.Instance.HipHeightToGround:F2}m | Knee: {PlayerTungkuTracker.Instance.KneeAngle:F0}°";
                postureStatusText.color = isSittingOrSquatting ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.6f, 0f);
            }
            return;
        }

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
            string logEntry = $"{currentTime},{currentUsername},{currentState},{activeTriggerName},{activeTriggerDuration:F1}," +
                              $"{distanceMeters:F2},{distanceStuds:F2},{distanceCentimeters:F1},{distanceFeet:F2}," +
                              $"{deltaXYZ_Meters.x:F2},{deltaXYZ_Meters.y:F2},{deltaXYZ_Meters.z:F2}," +
                              $"{deltaXYZ_Studs.x:F2},{deltaXYZ_Studs.y:F2},{deltaXYZ_Studs.z:F2}," +
                              $"{playerWorldPos.x:F2},{playerWorldPos.y:F2},{playerWorldPos.z:F2}," +
                              $"{tungkuWorldPos.x:F2},{tungkuWorldPos.y:F2},{tungkuWorldPos.z:F2}\n";
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