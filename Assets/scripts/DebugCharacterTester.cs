using UnityEngine;

/// <summary>
/// Komponen Debug untuk mengontrol karakter menggunakan WASD dan tombol Jongkok
/// tanpa memerlukan hardware Kinect (sangat berguna untuk testing dan presentasi).
/// </summary>
public class DebugCharacterTester : MonoBehaviour
{
    [Header("Target Karakter & Tracker")]
    [Tooltip("Transform avatar player yang akan digerakkan. Jika kosong, otomatis mengambil dari PlayerTungkuTracker.")]
    public Transform playerTransform;

    [Tooltip("Referensi ke PlayerTungkuTracker. Jika kosong, otomatis mencari di GameObject ini.")]
    public PlayerTungkuTracker tungkuTracker;

    [Header("Status Debug")]
    [Tooltip("Aktifkan kontrol keyboard WASD dan tombol Jongkok?")]
    public bool enableDebugControls = true;

    [Tooltip("Tampilkan tombol on-screen (GUI) di pojok layar?")]
    public bool showOnScreenGUI = true;

    [Header("Pengaturan Pergerakan (WASD)")]
    [Tooltip("Kecepatan jalan normal (meter/detik).")]
    public float moveSpeed = 2.0f;

    [Tooltip("Kecepatan lari saat menekan Left Shift.")]
    public float sprintSpeed = 4.0f;

    [Tooltip("Kecepatan rotasi karakter (derajat/detik) menggunakan tombol Q / E atau A / D.")]
    public float rotationSpeed = 90.0f;

    [Header("Pengaturan Jongkok (Crouch)")]
    [Tooltip("Tombol keyboard untuk Toggle Jongkok / Berdiri.")]
    public KeyCode crouchKey = KeyCode.C;

    [Tooltip("Tombol alternatif untuk Jongkok.")]
    public KeyCode alternateCrouchKey = KeyCode.LeftControl;

    [Tooltip("Berapa meter avatar diturunkan ke bawah saat jongkok.")]
    public float crouchYOffset = 0.55f;

    [Tooltip("Kecepatan transisi tinggi badan saat jongkok/berdiri.")]
    public float crouchTransitionSpeed = 8.0f;

    [Header("Status Realtime (Monitor)")]
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private float standingY = 0f;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Quaternion startRotation;

    public bool IsCrouching => isCrouching;

    void Awake()
    {
        FindReferences();
    }

    void Start()
    {
        FindReferences();

        if (playerTransform != null)
        {
            startPosition = playerTransform.position;
            startRotation = playerTransform.rotation;
            standingY = playerTransform.position.y;
        }
    }

    private void FindReferences()
    {
        if (tungkuTracker == null)
        {
            tungkuTracker = GetComponent<PlayerTungkuTracker>();
            if (tungkuTracker == null)
            {
                tungkuTracker = FindObjectOfType<PlayerTungkuTracker>();
            }
        }

        if (playerTransform == null && tungkuTracker != null)
        {
            playerTransform = tungkuTracker.playerTransform;
        }
    }

    void Update()
    {
        if (!enableDebugControls || playerTransform == null) return;

        // 1. Input Keyboard untuk Jongkok (Tombol C atau Left Control)
        if (Input.GetKeyDown(crouchKey) || Input.GetKeyDown(alternateCrouchKey))
        {
            ToggleCrouch();
        }

        // 2. Input Keyboard WASD & Arrow Keys untuk Gerak
        float horizontal = Input.GetAxis("Horizontal"); // A/D atau Panah Kiri/Kanan
        float vertical = Input.GetAxis("Vertical");     // W/S atau Panah Atas/Bawah

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        // Vektor pergerakan relatif terhadap hadap karakter atau kamera dunia
        Vector3 moveDirection = (playerTransform.forward * vertical + playerTransform.right * horizontal);
        moveDirection.y = 0f;
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            moveDirection.Normalize();
            playerTransform.position += moveDirection * (currentSpeed * Time.deltaTime);
        }

        // 3. Rotasi Karakter (Q = Putar Kiri, E = Putar Kanan)
        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotationInput -= 1f;
        if (Input.GetKey(KeyCode.E)) rotationInput += 1f;

        if (Mathf.Abs(rotationInput) > 0.01f)
        {
            playerTransform.Rotate(Vector3.up, rotationInput * rotationSpeed * Time.deltaTime, Space.World);
        }

        // 4. Transisi Ketinggian Y (Visual Jongkok)
        float targetY = isCrouching ? (standingY - crouchYOffset) : standingY;
        Vector3 curPos = playerTransform.position;
        curPos.y = Mathf.Lerp(curPos.y, targetY, Time.deltaTime * crouchTransitionSpeed);
        playerTransform.position = curPos;

        // 5. Sinkronisasi status ke PlayerTungkuTracker
        if (tungkuTracker != null)
        {
            if (tungkuTracker.debugSimulateJongkok != isCrouching)
            {
                tungkuTracker.debugSimulateJongkok = isCrouching;
            }
        }
    }

    /// <summary>
    /// Toggle status Jongkok (Sitting) vs Berdiri (Standing).
    /// </summary>
    public void ToggleCrouch()
    {
        SetCrouch(!isCrouching);
    }

    public void SetCrouch(bool crouch)
    {
        isCrouching = crouch;

        if (tungkuTracker != null)
        {
            tungkuTracker.debugSimulateJongkok = isCrouching;
            tungkuTracker.UpdateUI();
        }
    }

    /// <summary>
    /// Reset posisi dan rotasi avatar ke posisi awal saat scene dimulai.
    /// </summary>
    public void ResetPosition()
    {
        if (playerTransform != null)
        {
            playerTransform.position = startPosition;
            playerTransform.rotation = startRotation;
            standingY = startPosition.y;
            SetCrouch(false);
        }
    }

    // ==========================================
    // ON-SCREEN DEBUG CONTROLS (GUI)
    // ==========================================
    void OnGUI()
    {
        if (!enableDebugControls || !showOnScreenGUI) return;

        // Panel Kontrol di Kanan Atas Layar
        int panelWidth = 260;
        int panelHeight = 240;
        int marginX = Screen.width - panelWidth - 20;
        int marginY = 20;

        GUI.Box(new Rect(marginX, marginY, panelWidth, panelHeight), "<b>[DEBUG CHAR CONTROLLER]</b>");

        GUILayout.BeginArea(new Rect(marginX + 10, marginY + 25, panelWidth - 20, panelHeight - 35));

        // Info Status
        string postureStr = isCrouching ? "<color=#00FFFF><b>JONGKOK (Crouching)</b></color>" : "<color=#90EE90><b>BERDIRI (Standing)</b></color>";
        GUILayout.Label($"Status: {postureStr}");

        if (tungkuTracker != null)
        {
            GUILayout.Label($"Jarak ke Tungku: <b>{tungkuTracker.Distance:F2} m</b>");
            GUILayout.Label($"Hadap: {tungkuTracker.FacingCamera}");
        }

        GUILayout.Space(6);

        // Tombol Toggle Jongkok
        GUI.backgroundColor = isCrouching ? new Color(1f, 0.4f, 0.4f) : new Color(0.3f, 0.9f, 0.4f);
        string btnText = isCrouching ? ">> BERDIRI (Tekan C) <<" : ">> JONGKOK (Tekan C) <<";
        if (GUILayout.Button(btnText, GUILayout.Height(38)))
        {
            ToggleCrouch();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(4);

        // Tombol Reset Posisi
        if (GUILayout.Button("Reset Posisi Awal", GUILayout.Height(26)))
        {
            ResetPosition();
        }

        GUILayout.Space(6);
        GUI.contentColor = Color.yellow;
        GUILayout.Label("<size=10>Kontrol: WASD/Panah = Jalan\nQ/E = Putar Hadap | C = Jongkok\nShift = Lari</size>");
        GUI.contentColor = Color.white;

        GUILayout.EndArea();
    }
}
