using UnityEngine;
using TMPro;

[ExecuteAlways]
public class PlayerTungkuTracker : MonoBehaviour
{
    [Header("1. Pop Up Info Saat Jongkok: Text Nama Posisi")]
    [Tooltip("Panel pop up saat jongkok: 'Panel Kiri Bawahpop up informasi saat jongkok: text nama posisi'")]
    public GameObject panelInfoPosisiJongkok;
    [Tooltip("Text TMP di dalam panel info jongkok: 'TMP YANG DI GANTI'")]
    public TMP_Text textInfoPosisiJongkok;

    [Header("2. Counter: Berapa Lama di Posisi Jongkok")]
    [Tooltip("Panel counter durasi jongkok: 'Panel Kiri Berapa Lama Jongkok'")]
    public GameObject panelCounterJongkok;
    [Tooltip("Text TMP counter durasi: 'TMP YANG DI GANTI'")]
    public TMP_Text textCounterJongkok;

    [Header("3 & 4. Deteksi & Pop Up Jarak ke Tungku")]
    [Tooltip("Panel pop up jarak jongkok ke tungku: 'Panel Kiri Bawah Jongkok'")]
    public GameObject panelJarakTungkuJongkok;
    [Tooltip("Text TMP jarak ke tungku saat jongkok: 'TMP YANG DI GANTI'")]
    public TMP_Text textJarakTungkuJongkok;

    [Header("Posisi Arah Player (Umum)")]
    [Tooltip("Panel arah player: 'Panel Kiri Bawah Posisi Player'")]
    public GameObject panelPosisiArahPlayer;
    [Tooltip("Text TMP arah player: 'TMP YANG DI GANTI'")]
    public TMP_Text textPosisiArahPlayer;

    [Header("Target Transforms (Public)")]
    [Tooltip("Tarik Transform Player kamu ke sini.")]
    public Transform playerTransform;

    [Tooltip("Tarik Transform Tungku kamu ke sini.")]
    public Transform tungkuTransform;

    [Header("Sensor / Real Life Camera")]
    [Tooltip("Kamera sensor Kinect di dunia nyata. Jika kosong, otomatis memakai Camera.main.")]
    public Camera sensorCamera;

    [Header("Legacy Tracker Panel (Detail Lengkap)")]
    [Tooltip("Panel UI informasi detail lengkap (Panel Kiri Bawah).")]
    public GameObject trackerPanel;
    [Tooltip("TextMeshPro Text component untuk informasi detail lengkap.")]
    public TMP_Text infoText;

    [Header("Pop-Up & Counter Settings")]
    [Tooltip("Apakah panel 1, 2, dan 3&4 hanya muncul (pop up) saat player jongkok? Jika false, selalu tampil.")]
    public bool showPopupsOnlyWhenJongkok = true;

    [Tooltip("Reset counter waktu jongkok ke 0 saat player berdiri kembali?")]
    public bool resetCounterOnStand = true;

    [Tooltip("Tampilkan panel hanya saat sesi Kinect aktif? Jika false, selalu tampil.")]
    public bool showOnlyDuringActiveSession = false;

    [Tooltip("Batas sudut derajat untuk menentukan apakah player Menghadap ke Sensor/Kamera (Depan). Default: 45.")]
    [Range(10f, 80f)]
    public float facingThresholdAngle = 45f;

    [Header("Debug & Testing")]
    [Tooltip("Centang ini untuk mensimulasikan posisi Jongkok di Editor tanpa perlu hardware Kinect!")]
    public bool debugSimulateJongkok = false;

    [Header("Live Data / Inspector Monitor")]
    [SerializeField] private bool isJongkok = false;
    [SerializeField] private float currentJongkokDuration = 0f;
    [SerializeField] private string formattedDuration = "0:00";
    [SerializeField] private float currentDistance;
    [SerializeField] private Vector3 currentDeltaXYZ;
    [SerializeField] private Vector3 currentPlayerPos;
    [SerializeField] private Vector3 currentTungkuPos;
    [SerializeField] private float currentAngleToCamera;
    [SerializeField] private string currentFacingCamera = "Depan (Menghadap Kamera)";
    [SerializeField] private string currentPosture = "Standing";

    // Public getters untuk script eksternal
    public bool IsJongkok => isJongkok;
    public float JongkokDuration => currentJongkokDuration;
    public float Distance => currentDistance;
    public Vector3 DeltaXYZ => currentDeltaXYZ;
    public Vector3 PlayerPosition => currentPlayerPos;
    public Vector3 TungkuPosition => currentTungkuPos;
    public float AngleToCamera => currentAngleToCamera;
    public string FacingCamera => currentFacingCamera;
    public string Posture => currentPosture;

    void Awake()
    {
        AutoFindReferences();
    }

    void Start()
    {
        AutoFindReferences();

        if (sensorCamera == null)
        {
            sensorCamera = Camera.main;
        }

        UpdateTrackingData();
        UpdateUI();
    }

    void OnValidate()
    {
        AutoFindReferences();
    }

    void Reset()
    {
        AutoFindReferences();
    }

    void Update()
    {
        // 1. Cek sesi aktif jika opsi showOnlyDuringActiveSession diaktifkan
        bool isSessionActive = true;
        if (showOnlyDuringActiveSession && KinectSessionManager.Instance != null)
        {
            isSessionActive = KinectSessionManager.Instance.IsSessionActive;
            if (trackerPanel != null && trackerPanel.activeSelf != isSessionActive)
            {
                trackerPanel.SetActive(isSessionActive);
            }

            if (!isSessionActive)
            {
                SetAllJongkokPopupsActive(false);
                if (panelPosisiArahPlayer != null) panelPosisiArahPlayer.SetActive(false);
                return;
            }
        }

        // 2. Evaluasi status postur (Jongkok / Berdiri)
        EvaluatePosture();

        // 3. Counter durasi jongkok (hanya bertambah saat Runtime/Play mode)
        if (Application.isPlaying)
        {
            if (isJongkok)
            {
                currentJongkokDuration += Time.deltaTime;
            }
            else
            {
                if (resetCounterOnStand)
                {
                    currentJongkokDuration = 0f;
                }
            }
        }

        int minutes = Mathf.FloorToInt(currentJongkokDuration / 60f);
        int seconds = Mathf.FloorToInt(currentJongkokDuration % 60f);
        formattedDuration = string.Format("{0}:{1:00}", minutes, seconds);

        // 4. Update data posisi, hadap kamera, dan jarak ke tungku
        UpdateTrackingData();

        // 5. Update teks dan visibilitas UI
        UpdateUI();
    }

    /// <summary>
    /// Menentukan apakah user sedang dalam postur Jongkok (Sitting) atau Berdiri (Standing).
    /// </summary>
    private void EvaluatePosture()
    {
        if (debugSimulateJongkok)
        {
            currentPosture = "Sitting";
            isJongkok = true;
            return;
        }

        if (KinectSessionManager.Instance != null && !string.IsNullOrEmpty(KinectSessionManager.Instance.CurrentState))
        {
            currentPosture = KinectSessionManager.Instance.CurrentState;
            isJongkok = (currentPosture == "Sitting");
            return;
        }

        // Fallback jika tidak ada KinectSessionManager
        isJongkok = (currentPosture == "Sitting" || currentPosture == "Jongkok");
    }

    /// <summary>
    /// Menghitung posisi player, orientasi hadap, dan jarak ke tungku.
    /// </summary>
    private void UpdateTrackingData()
    {
        if (sensorCamera == null)
        {
            sensorCamera = Camera.main;
        }

        if (playerTransform != null)
        {
            currentPlayerPos = playerTransform.position;

            // Orientasi hadap kamera Kinect
            Vector3 playerForward = playerTransform.forward;
            playerForward.y = 0f;
            if (playerForward.sqrMagnitude > 0.0001f) playerForward.Normalize();
            else playerForward = Vector3.forward;

            bool detectedFromKinectHardware = false;

            if (KinectManager.Instance != null && KinectManager.Instance.IsInitialized())
            {
                long userId = KinectManager.Instance.GetUserIdByIndex(0);
                if (userId != 0)
                {
                    bool isTurnedAround = KinectManager.Instance.IsUserTurnedAround(userId);
                    if (isTurnedAround)
                    {
                        currentFacingCamera = "Membelakangi Kamera";
                        currentAngleToCamera = 180f;
                    }
                    else
                    {
                        currentFacingCamera = "Menghadap Kamera";
                        currentAngleToCamera = 0f;
                    }
                    detectedFromKinectHardware = true;
                }
            }

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
                    currentFacingCamera = "Menghadap Kamera";
                }
                else if (currentAngleToCamera >= (180f - facingThresholdAngle))
                {
                    currentFacingCamera = "Membelakangi Kamera";
                }
                else
                {
                    Vector3 cross = Vector3.Cross(playerForward, toCamera);
                    currentFacingCamera = cross.y > 0
                        ? "Menyamping Kiri"
                        : "Menyamping Kanan";
                }
            }
        }

        if (playerTransform != null && tungkuTransform != null)
        {
            currentTungkuPos = tungkuTransform.position;
            currentDeltaXYZ = currentTungkuPos - currentPlayerPos;
            currentDistance = Vector3.Distance(currentPlayerPos, currentTungkuPos);
        }
    }

    /// <summary>
    /// Memperbarui isi TextMeshPro dan visibilitas (Pop Up) dari setiap panel.
    /// </summary>
    public void UpdateUI()
    {
        bool shouldShowJongkokPopups = !showPopupsOnlyWhenJongkok || isJongkok;

        // 1. POP UP INFORMASI SAAT JONGKOK: TEXT NAMA POSISI
        if (panelInfoPosisiJongkok != null && panelInfoPosisiJongkok.activeSelf != shouldShowJongkokPopups)
        {
            panelInfoPosisiJongkok.SetActive(shouldShowJongkokPopups);
        }
        if (textInfoPosisiJongkok != null)
        {
            // Tampilkan nama posisi (misal: "Jongkok (Menghadap Kamera)")
            string posName = isJongkok ? $"Jongkok ({currentFacingCamera})" : $"Berdiri ({currentFacingCamera})";
            textInfoPosisiJongkok.text = posName;
        }

        // 2. COUNTER: BERAPA LAMA DI POSISI JONGKOK
        if (panelCounterJongkok != null && panelCounterJongkok.activeSelf != shouldShowJongkokPopups)
        {
            panelCounterJongkok.SetActive(shouldShowJongkokPopups);
        }
        if (textCounterJongkok != null)
        {
            textCounterJongkok.text = formattedDuration;
        }

        // 3 & 4. DETEKSI LOKASI JONGKOK JARAKNYA DARI TUNGKU & POP UP TEXT TERKAIT JARAK
        if (panelJarakTungkuJongkok != null && panelJarakTungkuJongkok.activeSelf != shouldShowJongkokPopups)
        {
            panelJarakTungkuJongkok.SetActive(shouldShowJongkokPopups);
        }
        if (textJarakTungkuJongkok != null)
        {
            if (tungkuTransform != null)
            {
                textJarakTungkuJongkok.text = $"{currentDistance:F2} m";
            }
            else
            {
                textJarakTungkuJongkok.text = "-";
            }
        }

        // POSISI ARAH PLAYER UMUM
        if (panelPosisiArahPlayer != null && !panelPosisiArahPlayer.activeSelf)
        {
            panelPosisiArahPlayer.SetActive(true);
        }
        if (textPosisiArahPlayer != null)
        {
            textPosisiArahPlayer.text = currentFacingCamera;
        }

        // LEGACY TRACKER PANEL (Jika ada)
        UpdateLegacyInfoText();
    }

    private void SetAllJongkokPopupsActive(bool active)
    {
        if (panelInfoPosisiJongkok != null) panelInfoPosisiJongkok.SetActive(active);
        if (panelCounterJongkok != null) panelCounterJongkok.SetActive(active);
        if (panelJarakTungkuJongkok != null) panelJarakTungkuJongkok.SetActive(active);
    }

    private void UpdateLegacyInfoText()
    {
        if (infoText == null) return;

        if (playerTransform == null)
        {
            infoText.text = "<color=#FFA500><b>[TRACKER TUNGKU]</b></color>\n" +
                            "<color=#FFFF77><i>Player Transform belum dimasukkan.</i></color>";
            return;
        }

        string postureDisplay = (currentPosture == "Sitting")
            ? "<color=#00E5FF>Jongkok / Duduk (Sitting)</color>"
            : "<color=#88FF88>Berdiri (Standing)</color>";

        if (tungkuTransform == null)
        {
            infoText.text = "<size=110%><b><color=#FFA500>INFO PLAYER</color></b></size>\n\n" +
                            $"<b>Hadap Kamera Kinect:</b> {currentFacingCamera}\n" +
                            $"<b>Status Postur:</b> {postureDisplay}\n" +
                            $"<b>Player XYZ:</b> <color=#FFE082>X: {currentPlayerPos.x:F2} | Y: {currentPlayerPos.y:F2} | Z: {currentPlayerPos.z:F2}</color>";
            return;
        }

        string deltaXStr = FormatSigned(currentDeltaXYZ.x);
        string deltaYStr = FormatSigned(currentDeltaXYZ.y);
        string deltaZStr = FormatSigned(currentDeltaXYZ.z);

        infoText.text =
            $"<b>Jarak ke Tungku:</b> <color=#00E5FF>{currentDistance:F2} m</color>\n" +
            $"<b>Durasi Jongkok:</b> <color=#FFE082>{formattedDuration}</color>\n" +
            $"<b>Selisih (ΔXYZ):</b> <color=#FFE082>ΔX: {deltaXStr}m | ΔY: {deltaYStr}m | ΔZ: {deltaZStr}m</color>\n" +
            $"<b>Hadap Kamera Kinect:</b> {currentFacingCamera}\n" +
            $"<b>Status Postur:</b> {postureDisplay}\n" +
            $"<b>Player XYZ:</b> <color=#B0BEC5>X: {currentPlayerPos.x:F2} | Y: {currentPlayerPos.y:F2} | Z: {currentPlayerPos.z:F2}</color>";
    }

    /// <summary>
    /// Mencari referensi GameObject dan TextMeshPro secara otomatis berdasarkan hierarki scene Unity.
    /// </summary>
    [ContextMenu("Auto-Find Hierarchy References")]
    public void AutoFindReferences()
    {
        // 1. Panel & TMP Info Posisi Jongkok
        if (panelInfoPosisiJongkok == null)
        {
            panelInfoPosisiJongkok = FindObjectInScene("Panel Kiri Bawahpop up informasi saat jongkok: text nama posisi");
        }
        if (panelInfoPosisiJongkok != null && textInfoPosisiJongkok == null)
        {
            textInfoPosisiJongkok = FindTmpChild(panelInfoPosisiJongkok, "TMP YANG DI GANTI");
        }

        // 2. Panel & TMP Counter Jongkok
        if (panelCounterJongkok == null)
        {
            panelCounterJongkok = FindObjectInScene("Panel Kiri Berapa Lama Jongkok");
        }
        if (panelCounterJongkok != null && textCounterJongkok == null)
        {
            textCounterJongkok = FindTmpChild(panelCounterJongkok, "TMP YANG DI GANTI");
        }

        // 3 & 4. Panel & TMP Jarak ke Tungku
        if (panelJarakTungkuJongkok == null)
        {
            panelJarakTungkuJongkok = FindObjectInScene("Panel Kiri Bawah Jongkok");
        }
        if (panelJarakTungkuJongkok != null && textJarakTungkuJongkok == null)
        {
            textJarakTungkuJongkok = FindTmpChild(panelJarakTungkuJongkok, "TMP YANG DI GANTI");
        }

        // Posisi Arah Player Umum
        if (panelPosisiArahPlayer == null)
        {
            panelPosisiArahPlayer = FindObjectInScene("Panel Kiri Bawah Posisi Player");
        }
        if (panelPosisiArahPlayer != null && textPosisiArahPlayer == null)
        {
            textPosisiArahPlayer = FindTmpChild(panelPosisiArahPlayer, "TMP YANG DI GANTI");
        }

        // Legacy tracker panel
        if (trackerPanel == null)
        {
            trackerPanel = FindObjectInScene("Panel Kiri Bawah");
        }
        if (trackerPanel != null && infoText == null)
        {
            TMP_Text[] tmps = trackerPanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps)
            {
                if (t.name.Contains("(3)")) { infoText = t; break; }
            }
        }

        // Tungku Transform
        if (tungkuTransform == null)
        {
            GameObject tungku = FindObjectInScene("Tungku");
            if (tungku != null) tungkuTransform = tungku.transform;
        }

        // Sensor Camera
        if (sensorCamera == null)
        {
            sensorCamera = Camera.main;
        }
    }

    private GameObject FindObjectInScene(string name)
    {
        // 1. Coba GameObject.Find langsung
        GameObject go = GameObject.Find(name);
        if (go != null) return go;

        // 2. Jika inactive, iterasi root objects di active scene
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isLoaded)
        {
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                Transform t = FindTransformRecursive(root.transform, name);
                if (t != null) return t.gameObject;
            }
        }
        return null;
    }

    private Transform FindTransformRecursive(Transform parent, string targetName)
    {
        if (parent == null) return null;
        if (parent.name == targetName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindTransformRecursive(parent.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }

    private TMP_Text FindTmpChild(GameObject parentGo, string tmpChildName)
    {
        if (parentGo == null) return null;
        Transform childTr = FindTransformRecursive(parentGo.transform, tmpChildName);
        if (childTr != null)
        {
            TMP_Text tmp = childTr.GetComponent<TMP_Text>();
            if (tmp != null) return tmp;
        }

        // Fallback: ambil TMP_Text yang bukan Header
        TMP_Text[] allTmps = parentGo.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in allTmps)
        {
            if (t.name.Contains("TMP YANG DI GANTI") || t.name == tmpChildName)
                return t;
        }
        return allTmps.Length > 0 ? allTmps[allTmps.Length - 1] : null;
    }

    /// <summary>
    /// Menuliskan contoh teks placeholder langsung ke seluruh TMP_Text di Editor.
    /// </summary>
    [ContextMenu("Tulis Debug Placeholder Text")]
    public void SetDebugPlaceholderText()
    {
        AutoFindReferences();

        if (textInfoPosisiJongkok != null)
            textInfoPosisiJongkok.text = "Jongkok (Menghadap Kamera)";

        if (textCounterJongkok != null)
            textCounterJongkok.text = "0:15";

        if (textJarakTungkuJongkok != null)
            textJarakTungkuJongkok.text = "1.85 m";

        if (textPosisiArahPlayer != null)
            textPosisiArahPlayer.text = "Menghadap Kamera";

        if (infoText != null)
        {
            infoText.text =
                "<b>Jarak ke Tungku:</b> <color=#00E5FF>1.85 m</color>\n" +
                "<b>Durasi Jongkok:</b> <color=#FFE082>0:15</color>\n" +
                "<b>Selisih (ΔXYZ):</b> <color=#FFE082>ΔX: +0.45m | ΔY: -0.10m | ΔZ: +1.79m</color>\n" +
                "<b>Hadap Kamera Kinect:</b> <color=#88FF88>Menghadap Kamera</color>\n" +
                "<b>Status Postur:</b> <color=#00E5FF>Jongkok / Duduk (Sitting)</color>";
        }

#if UNITY_EDITOR
        MarkDirty(textInfoPosisiJongkok);
        MarkDirty(textCounterJongkok);
        MarkDirty(textJarakTungkuJongkok);
        MarkDirty(textPosisiArahPlayer);
        MarkDirty(infoText);
#endif

        Debug.Log("[PlayerTungkuTracker] Placeholder debug text berhasil ditulis ke seluruh TMP Text!");
    }

#if UNITY_EDITOR
    private void MarkDirty(Component comp)
    {
        if (comp == null) return;
        UnityEditor.EditorUtility.SetDirty(comp);
        if (comp.gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
        }
    }
#endif

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

        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("Auto-Find References dari Scene Hierarchy", GUILayout.Height(30)))
        {
            tracker.AutoFindReferences();
            UnityEditor.EditorUtility.SetDirty(tracker);
        }

        GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
        if (GUILayout.Button("Tulis Debug Placeholder Text ke TMP", GUILayout.Height(30)))
        {
            tracker.SetDebugPlaceholderText();
        }

        GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
        string toggleText = tracker.debugSimulateJongkok ? "Simulasi Jongkok: [ON] (Klik utk Matikan)" : "Simulasi Jongkok: [OFF] (Klik utk Aktifkan)";
        if (GUILayout.Button(toggleText, GUILayout.Height(28)))
        {
            tracker.debugSimulateJongkok = !tracker.debugSimulateJongkok;
            tracker.UpdateUI();
            UnityEditor.EditorUtility.SetDirty(tracker);
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif
