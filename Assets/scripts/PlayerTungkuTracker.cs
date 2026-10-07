using UnityEngine;
using TMPro;

public class PlayerTungkuTracker : MonoBehaviour
{
    [Header("Target Transforms (Public - Masukkan via Inspector)")]
    [Tooltip("Tarik Transform Player kamu ke sini.")]
    public Transform playerTransform;

    [Tooltip("Tarik Transform Tungku kamu ke sini.")]
    public Transform tungkuTransform;

    [Header("Sensor / Real Life Camera")]
    [Tooltip("Kamera sensor Kinect di dunia nyata. Jika dibiarkan kosong, otomatis memakai Camera.main.")]
    public Camera sensorCamera;

    [Header("UI Display Elements")]
    [Tooltip("Panel UI untuk menampilkan informasi tracking ini.")]
    public GameObject trackerPanel;

    [Tooltip("TextMeshPro Text component untuk menampilkan teks Jarak, Arah, dan XYZ.")]
    public TMP_Text infoText;

    [Header("Settings")]
    [Tooltip("Tampilkan panel hanya saat sesi Kinect aktif? Jika false, selalu tampil.")]
    public bool showOnlyDuringActiveSession = false;

    [Tooltip("Batas sudut derajat untuk menentukan apakah player Menghadap ke Sensor/Kamera (Depan). Default: 45 derajat.")]
    [Range(10f, 80f)]
    public float facingThresholdAngle = 45f;

    [Header("Live Data / Variable XYZ (Inspector Monitor)")]
    [SerializeField] private float currentDistance;
    [SerializeField] private Vector3 currentDeltaXYZ;
    [SerializeField] private Vector3 currentPlayerPos;
    [SerializeField] private Vector3 currentTungkuPos;
    [SerializeField] private float currentAngleToCamera;
    [SerializeField] private string currentFacingCamera = "Depan (Menghadap Kamera)";
    [SerializeField] private string currentPosture = "Standing";

    // Public getters untuk akses eksternal jika dibutuhkan script lain
    public float Distance => currentDistance;
    public Vector3 DeltaXYZ => currentDeltaXYZ;
    public Vector3 PlayerPosition => currentPlayerPos;
    public Vector3 TungkuPosition => currentTungkuPos;
    public float AngleToCamera => currentAngleToCamera;
    public string FacingCamera => currentFacingCamera;
    public string Posture => currentPosture;

    void Start()
    {
        if (sensorCamera == null)
        {
            sensorCamera = Camera.main;
        }

        UpdateUI();
    }

    void Update()
    {
        // Cek visibilitas panel berdasarkan sesi jika opsi diaktifkan
        if (showOnlyDuringActiveSession && KinectSessionManager.Instance != null)
        {
            bool isSessionActive = KinectSessionManager.Instance.IsSessionActive;
            if (trackerPanel != null && trackerPanel.activeSelf != isSessionActive)
            {
                trackerPanel.SetActive(isSessionActive);
            }

            if (!isSessionActive) return;
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (infoText == null) return;

        // Auto find main camera jika belum diset
        if (sensorCamera == null)
        {
            sensorCamera = Camera.main;
        }

        // 1. Validasi referensi Player & Tungku
        if (playerTransform == null && tungkuTransform == null)
        {
            infoText.text = "<color=#FFA500><b>[TRACKER TUNGKU]</b></color>\n" +
                            "<color=#FFFF77><i>Player & Tungku Transform belum dimasukkan di Inspector.</i></color>";
            return;
        }

        if (playerTransform == null)
        {
            infoText.text = "<color=#FFA500><b>[TRACKER TUNGKU]</b></color>\n" +
                            "<color=#FFFF77><i>Player Transform belum dimasukkan di Inspector.</i></color>";
            return;
        }

        currentPlayerPos = playerTransform.position;
        string pXyzString = $"X: {currentPlayerPos.x:F2} | Y: {currentPlayerPos.y:F2} | Z: {currentPlayerPos.z:F2}";

        // 2. Deteksi Orientasi Hadap Player terhadap Kamera Kinect in Real Life (Depan / Belakang / Menyamping)
        Vector3 playerForward = playerTransform.forward;
        playerForward.y = 0f;
        if (playerForward.sqrMagnitude > 0.0001f) playerForward.Normalize();
        else playerForward = Vector3.forward;

        bool detectedFromKinectHardware = false;

        // Cek apakah ada data langsung dari Kinect Hardware
        if (KinectManager.Instance != null && KinectManager.Instance.IsInitialized())
        {
            long userId = KinectManager.Instance.GetUserIdByIndex(0);
            if (userId != 0)
            {
                bool isTurnedAround = KinectManager.Instance.IsUserTurnedAround(userId);
                if (isTurnedAround)
                {
                    currentFacingCamera = "<color=#FF8888>Belakang (Membelakangi Kamera)</color>";
                    currentAngleToCamera = 180f;
                }
                else
                {
                    currentFacingCamera = "<color=#88FF88>Depan (Menghadap Kamera)</color>";
                    currentAngleToCamera = 0f;
                }
                detectedFromKinectHardware = true;
            }
        }

        // Fallback: Hitung sudut relatif antara avatar player dengan Sensor Camera (bekerja di Editor & Play mode)
        if (!detectedFromKinectHardware && sensorCamera != null)
        {
            Vector3 camPos = sensorCamera.transform.position;
            Vector3 toCamera = camPos - currentPlayerPos;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude > 0.0001f) toCamera.Normalize();
            else toCamera = -sensorCamera.transform.forward;

            currentAngleToCamera = Vector3.Angle(playerForward, toCamera);

            if (currentAngleToCamera <= facingThresholdAngle)
            {
                currentFacingCamera = "<color=#88FF88>Depan (Menghadap Kamera)</color>";
            }
            else if (currentAngleToCamera >= (180f - facingThresholdAngle))
            {
                currentFacingCamera = "<color=#FF8888>Belakang (Membelakangi Kamera)</color>";
            }
            else
            {
                Vector3 cross = Vector3.Cross(playerForward, toCamera);
                currentFacingCamera = cross.y > 0
                    ? "<color=#88CCFF>Menyamping (Kamera di Kanan)</color>"
                    : "<color=#88CCFF>Menyamping (Kamera di Kiri)</color>";
            }
        }

        // 3. Ambil status postur (Jongkok / Berdiri) dari KinectSessionManager jika aktif
        if (KinectSessionManager.Instance != null && !string.IsNullOrEmpty(KinectSessionManager.Instance.CurrentState))
        {
            currentPosture = KinectSessionManager.Instance.CurrentState;
        }
        string postureDisplay = (currentPosture == "Sitting")
            ? "<color=#00E5FF>Jongkok / Duduk (Sitting)</color>"
            : "<color=#88FF88>Berdiri (Standing)</color>";

        // Jika tungku belum dimasukkan, tampilkan info player & orientasi kamera saja
        if (tungkuTransform == null)
        {
            infoText.text = "<size=110%><b><color=#FFA500>INFO PLAYER</color></b></size>\n\n" +
                            $"<b>Hadap Kamera Kinect:</b> {currentFacingCamera}\n" +
                            $"<b>Status Postur:</b> {postureDisplay}\n" +
                            $"<b>Player XYZ:</b> <color=#FFE082>{pXyzString}</color>\n\n" +
                            "<color=#FFFF77><i>Tungku Transform belum dimasukkan di Inspector.</i></color>";
            return;
        }

        // 4. Hitung posisi, jarak total, dan selisih XYZ (Tungku - Player)
        currentTungkuPos = tungkuTransform.position;
        currentDeltaXYZ = currentTungkuPos - currentPlayerPos;
        currentDistance = Vector3.Distance(currentPlayerPos, currentTungkuPos);

        string deltaXStr = FormatSigned(currentDeltaXYZ.x);
        string deltaYStr = FormatSigned(currentDeltaXYZ.y);
        string deltaZStr = FormatSigned(currentDeltaXYZ.z);

        // 5. Format tampilan lengkap
        infoText.text = 
            $"<b>Jarak ke Tungku:</b> <color=#00E5FF>{currentDistance:F2} m</color>\n" +
            $"<b>Selisih (ΔXYZ):</b> <color=#FFE082>ΔX: {deltaXStr}m | ΔY: {deltaYStr}m | ΔZ: {deltaZStr}m</color>\n" +
            $"<b>Hadap Kamera Kinect:</b> {currentFacingCamera}\n" +
            $"<b>Status Postur:</b> {postureDisplay}\n" +
            $"<b>Player XYZ:</b> <color=#B0BEC5>X: {currentPlayerPos.x:F2} | Y: {currentPlayerPos.y:F2} | Z: {currentPlayerPos.z:F2}</color>\n" +
            $"<b>Tungku XYZ:</b> <color=#B0BEC5>X: {currentTungkuPos.x:F2} | Y: {currentTungkuPos.y:F2} | Z: {currentTungkuPos.z:F2}</color>";
    }

    /// <summary>
    /// Menuliskan contoh teks placeholder langsung ke TMP_Text di Editor agar terlihat format & tampilannya di Canvas.
    /// </summary>
    [ContextMenu("Tulis Debug Placeholder Text")]
    public void SetDebugPlaceholderText()
    {
        if (infoText == null)
        {
            Debug.LogWarning("[PlayerTungkuTracker] infoText (TMP_Text) belum dimasukkan di Inspector!");
            return;
        }

        string placeholder = 
            "<b>Jarak ke Tungku:</b> <color=#00E5FF>1.85 m</color>\n" +
            "<b>Selisih (ΔXYZ):</b> <color=#FFE082>ΔX: +0.45m | ΔY: -0.10m | ΔZ: +1.79m</color>\n" +
            "<b>Hadap Kamera Kinect:</b> <color=#88FF88>Depan (Menghadap Kamera)</color>\n" +
            "<b>Status Postur:</b> <color=#88FF88>Berdiri (Standing)</color>\n" +
            "<b>Player XYZ:</b> <color=#B0BEC5>X: 0.12 | Y: 0.85 | Z: -1.50</color>\n" +
            "<b>Tungku XYZ:</b> <color=#B0BEC5>X: 0.57 | Y: 0.75 | Z: 0.29</color>";

        infoText.text = placeholder;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(infoText);
        if (infoText.gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(infoText.gameObject.scene);
        }
#endif

        Debug.Log("[PlayerTungkuTracker] Placeholder debug text berhasil ditulis ke TMP Text!");
    }

    private string FormatSigned(float val)
    {
        return val >= 0f ? $"+{val:F2}" : $"{val:F2}";
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(PlayerTungkuTracker))]
public class PlayerTungkuTrackerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerTungkuTracker tracker = (PlayerTungkuTracker)target;

        UnityEditor.EditorGUILayout.Space(12);
        UnityEditor.EditorGUILayout.LabelField("Debug Tools & Preview Editor", UnityEditor.EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
        if (GUILayout.Button("Tulis Debug Placeholder Text ke Editor", GUILayout.Height(32)))
        {
            tracker.SetDebugPlaceholderText();
        }

        GUI.backgroundColor = new Color(0.85f, 0.95f, 0.85f);
        if (GUILayout.Button("Update Teks dari Posisi Scene Saat Ini (Editor Mode)", GUILayout.Height(28)))
        {
            tracker.UpdateUI();
            if (tracker.infoText != null)
            {
                UnityEditor.EditorUtility.SetDirty(tracker.infoText);
                if (tracker.infoText.gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tracker.infoText.gameObject.scene);
                }
            }
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif


