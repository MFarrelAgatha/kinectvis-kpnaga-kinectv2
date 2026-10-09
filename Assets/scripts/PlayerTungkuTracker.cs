using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum PostureType
{
    Standing,        // Berdiri
    SittingChair,    // Duduk Kursi
    SittingFloor,    // Duduk Selonjoran
    Squatting        // Jongkok
}

public class PlayerTungkuTracker : MonoBehaviour
{
    [Header("1. Pop Up Info Saat Jongkok: Text Nama Posisi")]
    [Tooltip("Panel pop up saat jongkok: 'Panel Kiri Bawahpop up informasi saat jongkok: text nama posisi'")]
    public GameObject panelInfoPosisiJongkok;
    [Tooltip("Image component background dari panel info jongkok")]
    public Image imageInfoPosisiJongkok;
    [Tooltip("Text TMP di dalam panel info jongkok: 'TMP YANG DI GANTI'")]
    public TMP_Text textInfoPosisiJongkok;

    [Header("2. Counter: Berapa Lama di Posisi Jongkok")]
    [Tooltip("Panel counter durasi jongkok: 'Panel Kiri Berapa Lama Jongkok'")]
    public GameObject panelCounterJongkok;
    [Tooltip("Image component background dari panel counter jongkok")]
    public Image imageCounterJongkok;
    [Tooltip("Text TMP counter durasi: 'TMP YANG DI GANTI'")]
    public TMP_Text textCounterJongkok;

    [Header("3 & 4. Deteksi & Pop Up Jarak ke Tungku")]
    [Tooltip("Panel pop up jarak jongkok ke tungku: 'Panel Kiri Bawah Jongkok'")]
    public GameObject panelJarakTungkuJongkok;
    [Tooltip("Image component background dari panel jarak tungku jongkok")]
    public Image imageJarakTungkuJongkok;
    [Tooltip("Text TMP jarak ke tungku saat jongkok: 'TMP YANG DI GANTI'")]
    public TMP_Text textJarakTungkuJongkok;

    [Header("Posisi Arah Player (Umum)")]
    [Tooltip("Panel arah player: 'Panel Kiri Bawah Posisi Player'")]
    public GameObject panelPosisiArahPlayer;
    [Tooltip("Text TMP arah player: 'TMP YANG DI GANTI'")]
    public TMP_Text textPosisiArahPlayer;

    [Header("Panel Login & Shortcut Button")]
    [Tooltip("Panel Login ('Panel Tengah'). Jika kosong, otomatis mencari di scene atau dari KinectSessionManager.")]
    public GameObject panelLogin;
    [Tooltip("Tombol Button di UI untuk membuka panel login (misal Button di Panel Kiri Atas).")]
    public Button buttonBukaLogin;

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

    [Header("Status Warna Panel & Font (Nyala vs Mati)")]
    [Tooltip("Gunakan perubahan warna panel & font (panel akan selalu aktif/muncul, warna biru saat jongkok dan merah saat berdiri).")]
    public bool useColorStatusInsteadOfPopups = true;

    [Tooltip("Ubah semua teks font yang ada di dalam panel (termasuk label judul jika ada)?")]
    public bool changeAllFontsInPanel = true;

    [Tooltip("Warna background panel saat aktif JONGKOK (Default: Biru Tua).")]
    public Color colorPanelNyala = new Color(0f, 0.055f, 0.70f, 0.88f);

    [Tooltip("Warna background panel saat posisi DUDUK di kursi (Default: Cyan / Biru Muda).")]
    public Color colorPanelDuduk = new Color(0.0f, 0.55f, 0.88f, 0.88f);

    [Tooltip("Warna background panel saat mati / BERDIRI / tidak terdeteksi (Default: Merah).")]
    public Color colorPanelMati = new Color(0.72f, 0.08f, 0.08f, 0.88f);

    [Tooltip("Warna font saat aktif / jongkok (Default: Putih).")]
    public Color colorFontNyala = Color.white;

    [Tooltip("Warna font saat mati / berdiri (Default: Abu-abu).")]
    public Color colorFontMati = new Color(0.65f, 0.65f, 0.65f, 1f);

    [Header("Teks Saat Status Mati (Berdiri / Tidak Jongkok)")]
    [Tooltip("Teks status posisi saat mati / berdiri.")]
    public string textPosisiMati = "Tidak Terdeteksi (Berdiri)";

    [Tooltip("Teks counter durasi saat mati / berdiri.")]
    public string textCounterMati = "0:00 (Tidak Aktif)";

    [Tooltip("Teks jarak tungku saat mati / berdiri.")]
    public string textJarakMati = "Tidak Terdeteksi";

    [Header("Mode Otak-Atik Pop-Up (Legacy)")]
    [Tooltip("Centang ini jika ingin OTAK-ATIK pop-up secara BEBAS! Script tidak akan pernah mematikan/menyalakan panel secara paksa.")]
    public bool manualControlPopups = false;

    [Tooltip("Paksa SEMUA pop-up menyala terus saat Play mode.")]
    public bool forceShowAllPopups = false;

    [Header("Pop-Up & Counter Settings (Mode Pop-Up Legacy)")]
    [Tooltip("Apakah panel pop-up hanya muncul saat player jongkok? Jika false, selalu tampil.")]
    public bool showPopupsOnlyWhenJongkok = true;

    [Tooltip("Reset counter waktu jongkok ke 0 saat player berdiri kembali?")]
    public bool resetCounterOnStand = true;

    [Tooltip("Tampilkan panel hanya saat sesi Kinect aktif? Jika false, selalu tampil.")]
    public bool showOnlyDuringActiveSession = false;

    [Tooltip("Batas sudut derajat untuk menentukan apakah player Menghadap ke Sensor/Kamera (Depan). Default: 45.")]
    [Range(10f, 80f)]
    public float facingThresholdAngle = 45f;

    [Header("Floor Grounding (Avatar Menempel ke Lantai)")]
    [Tooltip("Aktifkan penyesuaian posisi avatar dengan Raycast ke bawah agar kaki selalu menempel di lantai dan tidak melayang saat jongkok.")]
    public bool enableFloorGrounding = true;

    [Tooltip("Layer tanah/lantai yang dideteksi oleh Raycast (Default: Semua solid collider).")]
    public LayerMask groundLayer = ~0;

    [Tooltip("Jarak maksimal Raycast ke bawah untuk mendeteksi lantai (meter).")]
    public float maxGroundRayDistance = 4.0f;

    [Tooltip("Tinggi asal tembakan Raycast di atas posisi kaki (meter).")]
    public float groundRayOriginOffset = 0.5f;

    [Tooltip("Offset ketebalan sol sepatu / telapak kaki agar tidak tenggelam ke dalam lantai (meter).")]
    public float footSoleOffset = 0.02f;

    [Tooltip("Kecepatan lerp penghalusan penempelan ke lantai. Isi 0 untuk langsung menempel seketika (snap instant).")]
    public float groundSmoothing = 0f;

    [Header("Deteksi Postur Fisik (Jongkok vs Duduk Kursi vs Selonjoran vs Berdiri)")]
    [Tooltip("Gunakan deteksi fisik postur berdasarkan ketinggian pinggul (Hips), sudut lutut, dan posisi kaki avatar di Unity.")]
    public bool enablePhysicalSquatDetection = true;

    [Tooltip("Batas tinggi pinggul (Hips) dari lantai (meter) untuk JONGKOK (Default: 0.55m).")]
    [Range(0.2f, 0.7f)]
    public float squatHipsHeightThreshold = 0.55f;

    [Tooltip("Batas tinggi pinggul di lantai (meter) untuk DUDUK SELONJORAN (Default: 0.38m).")]
    [Range(0.15f, 0.5f)]
    public float floorSittingHipThreshold = 0.38f;

    [Tooltip("Batas tinggi pinggul (Hips) dari lantai (meter) untuk DUDUK KURSI (Default: 0.78m).")]
    [Range(0.5f, 0.95f)]
    public float sitHipsHeightThreshold = 0.78f;

    [Tooltip("Batas jarak kompresi Pinggul ke Kaki (meter) untuk JONGKOK (Default: 0.52m).")]
    [Range(0.2f, 0.65f)]
    public float squatLegDistanceThreshold = 0.52f;

    [Tooltip("Batas maksimal jarak horizontal XZ antara pinggul dan kaki saat JONGKOK (Default: 0.42m).")]
    [Range(0.2f, 0.6f)]
    public float maxSquatHorizontalDist = 0.42f;

    [Tooltip("Batas minimal jarak horizontal XZ kaki menjulur ke depan saat SELONJORAN (Default: 0.50m).")]
    [Range(0.35f, 0.9f)]
    public float minSelonjoranHorizontalDist = 0.50f;

    [Tooltip("Batas sudut tekukan lutut (derajat) untuk JONGKOK (lutut terlipat tajam < sudut ini, Default: 75°).")]
    [Range(30f, 90f)]
    public float squatKneeAngleThreshold = 75f;

    [Tooltip("Batas sudut tekukan lutut (derajat) untuk DUDUK KURSI (Default: 130°).")]
    [Range(70f, 150f)]
    public float sitKneeAngleThreshold = 130f;

    [Tooltip("Sinkronkan status postur ke KinectSessionManager agar UI status posture & data logger ikut tercatat.")]
    public bool syncWithKinectSessionManager = true;

    [Header("Debug & Testing")]
    [Tooltip("Centang ini untuk mensimulasikan posisi Jongkok di Editor tanpa perlu hardware Kinect!")]
    public bool debugSimulateJongkok = false;

    [Header("Live Grounding & Physical Monitor")]
    [SerializeField] private float liveHipHeightToGround = 0f;
    [SerializeField] private float liveHipToFootDistance = 0f;
    [SerializeField] private float liveHorizontalHipToFoot = 0f;
    [SerializeField] private float liveKneeAngle = 180f;
    [SerializeField] private bool liveRaycastHitGround = false;
    [SerializeField] private float liveGroundHitY = 0f;
    [SerializeField] private Vector3 liveLowestFootPos = Vector3.zero;
    [SerializeField] private PostureType currentPostureType = PostureType.Standing;

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
    public PostureType CurrentPostureType => currentPostureType;
    public float HipHeightToGround => liveHipHeightToGround;
    public float SquatHipsHeightThreshold => squatHipsHeightThreshold;
    public float SitHipsHeightThreshold => sitHipsHeightThreshold;
    public float HipToFootDistance => liveHipToFootDistance;
    public float HorizontalHipToFoot => liveHorizontalHipToFoot;
    public float KneeAngle => liveKneeAngle;
    public bool RaycastHitGround => liveRaycastHitGround;
    public float GroundHitY => liveGroundHitY;

    public string GetPostureLabelIndonesian()
    {
        switch (currentPostureType)
        {
            case PostureType.Squatting: return "Jongkok";
            case PostureType.SittingChair: return "Duduk Kursi";
            case PostureType.SittingFloor: return "Duduk Selonjoran";
            default: return "Berdiri";
        }
    }

    // Singleton instance
    public static PlayerTungkuTracker Instance { get; private set; }

    // Cached bone references
    private Animator playerAnimator;
    private Transform boneHips;
    private Transform boneLeftKnee;
    private Transform boneRightKnee;
    private Transform boneLeftFoot;
    private Transform boneRightFoot;
    private Transform boneLeftToe;
    private Transform boneRightToe;
    private Vector3 lastRayOrigin = Vector3.zero;
    private Vector3 lastGroundHitPoint = Vector3.zero;

    void Awake()
    {
        if (Instance == null) Instance = this;
        AutoFindReferences();
        CachePlayerBones();
    }

    void Start()
    {
        if (Instance == null) Instance = this;
        AutoFindReferences();
        CachePlayerBones();

        if (sensorCamera == null)
        {
            sensorCamera = Camera.main;
        }

        if (buttonBukaLogin != null)
        {
            buttonBukaLogin.onClick.RemoveListener(OpenLoginPanel);
            buttonBukaLogin.onClick.AddListener(OpenLoginPanel);
        }

        if (enableFloorGrounding)
        {
            ApplyFloorGrounding();
        }

        UpdateTrackingData();
        UpdateUI();
    }

    void OnValidate()
    {
        AutoFindReferences();
        CachePlayerBones();
    }

    void Reset()
    {
        AutoFindReferences();
        CachePlayerBones();
    }

    void Update()
    {
        // Cek sesi aktif jika opsi showOnlyDuringActiveSession diaktifkan (hanya jika bukan mode manual)
        bool isSessionActive = true;
        if (showOnlyDuringActiveSession && KinectSessionManager.Instance != null && !manualControlPopups)
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
    }

    void LateUpdate()
    {
        // 1. Tahan avatar ke lantai dengan Raycast ke bawah (dijalankan di LateUpdate setelah AvatarController selesai di Update)
        if (enableFloorGrounding && playerTransform != null)
        {
            ApplyFloorGrounding();
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
    /// Mencari dan menyimpan referensi bone-bone avatar (Hips, Kaki, dan Ujung Jari).
    /// </summary>
    public void CachePlayerBones()
    {
        if (playerTransform == null) return;

        playerAnimator = playerTransform.GetComponent<Animator>();
        if (playerAnimator != null && playerAnimator.isHuman)
        {
            boneHips = playerAnimator.GetBoneTransform(HumanBodyBones.Hips);
            boneLeftKnee = playerAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            boneRightKnee = playerAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            boneLeftFoot = playerAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            boneRightFoot = playerAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            boneLeftToe = playerAnimator.GetBoneTransform(HumanBodyBones.LeftToes);
            boneRightToe = playerAnimator.GetBoneTransform(HumanBodyBones.RightToes);
        }

        // Fallback: dari AvatarController jika ada
        if (boneHips == null || (boneLeftFoot == null && boneRightFoot == null))
        {
            AvatarController ac = playerTransform.GetComponent<AvatarController>();
            if (ac != null)
            {
                int hipIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.SpineBase, false);
                int kneeLIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.KneeLeft, false);
                int kneeRIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.KneeRight, false);
                int footLIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.FootLeft, false);
                int footRIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.FootRight, false);
                int ankleLIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.AnkleLeft, false);
                int ankleRIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.AnkleRight, false);

                if (boneHips == null && hipIdx >= 0) boneHips = ac.GetBoneTransform(hipIdx);
                if (boneLeftKnee == null && kneeLIdx >= 0) boneLeftKnee = ac.GetBoneTransform(kneeLIdx);
                if (boneRightKnee == null && kneeRIdx >= 0) boneRightKnee = ac.GetBoneTransform(kneeRIdx);
                if (boneLeftFoot == null) boneLeftFoot = (footLIdx >= 0 ? ac.GetBoneTransform(footLIdx) : null) ?? (ankleLIdx >= 0 ? ac.GetBoneTransform(ankleLIdx) : null);
                if (boneRightFoot == null) boneRightFoot = (footRIdx >= 0 ? ac.GetBoneTransform(footRIdx) : null) ?? (ankleRIdx >= 0 ? ac.GetBoneTransform(ankleRIdx) : null);
            }
        }

        // Fallback pencarian nama transform recursive
        if (boneHips == null) boneHips = FindChildRecursiveKeywords(playerTransform, "hip", "pelvis", "spine");
        if (boneLeftKnee == null) boneLeftKnee = FindChildRecursiveKeywords(playerTransform, "leftknee", "knee.l", "knee_l", "shin.l", "shin_l");
        if (boneRightKnee == null) boneRightKnee = FindChildRecursiveKeywords(playerTransform, "rightknee", "knee.r", "knee_r", "shin.r", "shin_r");
        if (boneLeftFoot == null) boneLeftFoot = FindChildRecursiveKeywords(playerTransform, "leftfoot", "foot.l", "foot_l", "ankle.l", "ankle_l");
        if (boneRightFoot == null) boneRightFoot = FindChildRecursiveKeywords(playerTransform, "rightfoot", "foot.r", "foot_r", "ankle.r", "ankle_r");
    }

    private float CalculateKneeAngle(Transform hip, Transform knee, Transform foot)
    {
        if (hip == null || knee == null || foot == null) return 180f;
        Vector3 thigh = (hip.position - knee.position).normalized;
        Vector3 calf = (foot.position - knee.position).normalized;
        return Vector3.Angle(thigh, calf);
    }

    private Transform FindChildRecursiveKeywords(Transform parent, params string[] keywords)
    {
        if (parent == null) return null;
        string pName = parent.name.ToLower();
        foreach (var kw in keywords)
        {
            if (pName.Contains(kw.ToLower())) return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursiveKeywords(parent.GetChild(i), keywords);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Menahan kaki avatar ke lantai dengan Raycast ke bawah sehingga avatar tidak pernah melayang di udara saat jongkok.
    /// </summary>
    [ContextMenu("Snap Avatar Ke Lantai Sekarang")]
    public void ApplyFloorGrounding()
    {
        if (playerTransform == null) return;
        if (boneHips == null || (boneLeftFoot == null && boneRightFoot == null))
        {
            CachePlayerBones();
        }

        // Cari posisi kaki / ujung jari terendah
        Vector3 lowestFootPos = playerTransform.position;
        float lowestY = float.MaxValue;
        bool foundFootBone = false;

        Transform[] feetToCheck = { boneLeftToe, boneRightToe, boneLeftFoot, boneRightFoot };
        foreach (Transform f in feetToCheck)
        {
            if (f != null)
            {
                float y = f.position.y;
                if (y < lowestY)
                {
                    lowestY = y;
                    lowestFootPos = f.position;
                    foundFootBone = true;
                }
            }
        }

        if (!foundFootBone)
        {
            lowestFootPos = playerTransform.position;
            lowestY = playerTransform.position.y;
        }

        liveLowestFootPos = lowestFootPos;

        // Tembakkan raycast dari sedikit di atas telapak kaki ke arah bawah
        Vector3 rayOrigin = new Vector3(lowestFootPos.x, lowestY + groundRayOriginOffset, lowestFootPos.z);
        lastRayOrigin = rayOrigin;

        // Dapatkan semua hit collider dan abaikan collider milik avatar sendiri
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, maxGroundRayDistance, groundLayer, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool hitGround = false;
        RaycastHit groundHit = default;

        foreach (var h in hits)
        {
            if (h.transform != playerTransform && !h.transform.IsChildOf(playerTransform))
            {
                groundHit = h;
                hitGround = true;
                break;
            }
        }

        if (hitGround)
        {
            liveRaycastHitGround = true;
            liveGroundHitY = groundHit.point.y;
            lastGroundHitPoint = groundHit.point;

            // Target Y kaki di permukaan lantai (ditambah offset sol sepatu)
            float targetFootY = groundHit.point.y + footSoleOffset;
            float deltaY = targetFootY - lowestY;

            if (Mathf.Abs(deltaY) > 0.001f)
            {
                Vector3 currentRoot = playerTransform.position;
                float targetRootY = currentRoot.y + deltaY;

                if (groundSmoothing > 0f && Application.isPlaying)
                {
                    currentRoot.y = Mathf.Lerp(currentRoot.y, targetRootY, Time.deltaTime * groundSmoothing);
                }
                else
                {
                    currentRoot.y = targetRootY;
                }

                playerTransform.position = currentRoot;
            }
        }
        else
        {
            liveRaycastHitGround = false;
        }
    }

    /// <summary>
    /// Menentukan apakah user sedang dalam postur Jongkok (Squatting), Duduk (Sitting), atau Berdiri (Standing).
    /// Menggunakan Raycast ketinggian tubuh, kompresi kaki, dan sudut tekukan lutut.
    /// </summary>
    private void EvaluatePosture()
    {
        if (debugSimulateJongkok)
        {
            currentPostureType = PostureType.Squatting;
            currentPosture = "Squatting";
            isJongkok = true;
            return;
        }

        // 1. Deteksi fisik via Raycast dan ketinggian/sudut tulang Avatar
        bool isSquattingPhysically = false;
        bool isSittingPhysically = false;

        if (enablePhysicalSquatDetection && boneHips != null)
        {
            // Tembakkan raycast dari Hips ke bawah untuk mengukur tinggi pinggul ke lantai
            Vector3 hipOrigin = boneHips.position + Vector3.up * 0.1f;
            RaycastHit[] hipHits = Physics.RaycastAll(hipOrigin, Vector3.down, 3.5f, groundLayer, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hipHits, (a, b) => a.distance.CompareTo(b.distance));

            bool foundFloorUnderHip = false;
            float floorYUnderHip = 0f;

            foreach (var hh in hipHits)
            {
                if (hh.transform != playerTransform && !hh.transform.IsChildOf(playerTransform))
                {
                    floorYUnderHip = hh.point.y;
                    foundFloorUnderHip = true;
                    break;
                }
            }

            if (foundFloorUnderHip)
            {
                liveHipHeightToGround = boneHips.position.y - floorYUnderHip;
            }
            else if (liveRaycastHitGround)
            {
                liveHipHeightToGround = boneHips.position.y - liveGroundHitY;
            }
            else
            {
                liveHipHeightToGround = boneHips.position.y;
            }

            // Hitung kompresi tekukan kaki (jarak Pinggul ke posisi kaki)
            Vector3 footRef = (boneLeftFoot != null && boneRightFoot != null)
                ? (boneLeftFoot.position + boneRightFoot.position) * 0.5f
                : (boneLeftFoot != null ? boneLeftFoot.position : (boneRightFoot != null ? boneRightFoot.position : liveLowestFootPos));

            liveHipToFootDistance = Vector3.Distance(boneHips.position, footRef);

            // Hitung jarak horizontal XZ antara Pinggul dan Kaki (krusial membedakan Duduk Selonjoran vs Jongkok)
            Vector2 hipXZ = new Vector2(boneHips.position.x, boneHips.position.z);
            Vector2 footXZ = new Vector2(footRef.x, footRef.z);
            liveHorizontalHipToFoot = Vector2.Distance(hipXZ, footXZ);

            // Hitung sudut lutut avatar
            float leftKneeAngle = CalculateKneeAngle(boneHips, boneLeftKnee, boneLeftFoot);
            float rightKneeAngle = CalculateKneeAngle(boneHips, boneRightKnee, boneRightFoot);
            liveKneeAngle = Mathf.Min(leftKneeAngle, rightKneeAngle);

            // 1. Kriteria: DUDUK SELONJORAN (SittingFloor)
            // Karakteristik: Pinggul menempel di lantai (< floorSittingHipThreshold ~0.38m)
            // DAN kaki menjulur ke depan (jarak horizontal XZ >= minSelonjoranHorizontalDist ~0.50m ATAU lutut lurus > 115° ATAU hipToFootDistance > squatLegDistanceThreshold)
            bool isFloorSitting = (liveHipHeightToGround > 0f && liveHipHeightToGround < floorSittingHipThreshold)
                && (liveHorizontalHipToFoot >= minSelonjoranHorizontalDist || liveKneeAngle > 115f || liveHipToFootDistance > squatLegDistanceThreshold);

            // 2. Kriteria: JONGKOK (Squatting)
            // Karakteristik: Pinggul rendah (< squatHipsHeightThreshold ~0.55m), lutut terlipat tajam (< squatKneeAngleThreshold ~75°),
            // dan kaki terlipat rapat di bawah/dekat pinggul (horizontal dist < maxSquatHorizontalDist ~0.42m)
            bool isSquatting = !isFloorSitting
                && (liveHipHeightToGround > 0f && liveHipHeightToGround < squatHipsHeightThreshold)
                && (liveHorizontalHipToFoot < maxSquatHorizontalDist)
                && (liveKneeAngle < squatKneeAngleThreshold || liveHipToFootDistance < squatLegDistanceThreshold);

            // 3. Kriteria: DUDUK KURSI (SittingChair)
            // Karakteristik: Pinggul di rentang kursi (< sitHipsHeightThreshold ~0.78m) dan lutut tertekuk wajar (< sitKneeAngleThreshold ~130°)
            bool isChairSitting = !isFloorSitting && !isSquatting
                && (liveHipHeightToGround > 0f && liveHipHeightToGround < sitHipsHeightThreshold)
                && (liveKneeAngle < sitKneeAngleThreshold);

            // Evaluasi Postur Fisik
            if (isSquatting)
            {
                currentPostureType = PostureType.Squatting;
                currentPosture = "Squatting";
                isJongkok = true;
            }
            else if (isFloorSitting)
            {
                currentPostureType = PostureType.SittingFloor;
                currentPosture = "SittingFloor";
                isJongkok = false;
            }
            else if (isChairSitting)
            {
                currentPostureType = PostureType.SittingChair;
                currentPosture = "SittingChair";
                isJongkok = false;
            }
            else
            {
                currentPostureType = PostureType.Standing;
                currentPosture = "Standing";
                isJongkok = false;
            }
        }
        else
        {
            // Fallback hanya jika avatar tidak memiliki tulang / deteksi fisik nonaktif
            if (KinectSessionManager.Instance != null)
            {
                string s = KinectSessionManager.Instance.CurrentState;
                if (s == "Squatting" || s == "Jongkok")
                {
                    currentPostureType = PostureType.Squatting;
                    currentPosture = "Squatting";
                    isJongkok = true;
                }
                else if (s == "SittingChair" || s == "Sitting")
                {
                    currentPostureType = PostureType.SittingChair;
                    currentPosture = "SittingChair";
                    isJongkok = false;
                }
                else if (s == "SittingFloor")
                {
                    currentPostureType = PostureType.SittingFloor;
                    currentPosture = "SittingFloor";
                    isJongkok = false;
                }
                else
                {
                    currentPostureType = PostureType.Standing;
                    currentPosture = "Standing";
                    isJongkok = false;
                }
            }
            else
            {
                currentPostureType = PostureType.Standing;
                currentPosture = "Standing";
                isJongkok = false;
            }
        }

        // 3. Sinkronkan ke KinectSessionManager
        if (syncWithKinectSessionManager && KinectSessionManager.Instance != null && Application.isPlaying)
        {
            string detail = (currentPostureType != PostureType.Standing)
                ? $"{GetPostureLabelIndonesian()} | Pinggul: {liveHipHeightToGround:F2}m | Lutut: {liveKneeAngle:F0}°"
                : "";
            KinectSessionManager.Instance.SetPostureState(currentPosture, detail);
        }
    }

    private void OnDrawGizmos()
    {
        if (playerTransform == null) return;

        // Gizmos Raycast Kaki ke Lantai (Grounding)
        if (enableFloorGrounding && liveRaycastHitGround)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(lastRayOrigin, lastGroundHitPoint);
            Gizmos.DrawWireSphere(lastGroundHitPoint, 0.05f);
        }

        // Gizmos Raycast Pinggul (Posture Detection)
        if (boneHips != null)
        {
            if (currentPostureType == PostureType.Squatting)
                Gizmos.color = Color.yellow;
            else if (currentPostureType == PostureType.SittingChair)
                Gizmos.color = new Color(0f, 0.75f, 1f); // Cyan
            else if (currentPostureType == PostureType.SittingFloor)
                Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            else
                Gizmos.color = new Color(0.3f, 0.85f, 0.3f); // Hijau (Standing)

            Gizmos.DrawLine(boneHips.position, boneHips.position + Vector3.down * liveHipHeightToGround);
            Gizmos.DrawWireSphere(boneHips.position, 0.08f);

            // Garis batas threshold selonjoran (Orange)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Vector3 floorSitPos = new Vector3(boneHips.position.x, boneHips.position.y - floorSittingHipThreshold, boneHips.position.z);
            Gizmos.DrawWireCube(floorSitPos, new Vector3(0.3f, 0.02f, 0.3f));

            // Garis batas threshold jongkok (Kuning)
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.6f);
            Vector3 squatThresholdPos = new Vector3(boneHips.position.x, boneHips.position.y - squatHipsHeightThreshold, boneHips.position.z);
            Gizmos.DrawWireCube(squatThresholdPos, new Vector3(0.35f, 0.02f, 0.35f));

            // Garis batas threshold duduk di kursi (Cyan)
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f);
            Vector3 sitThresholdPos = new Vector3(boneHips.position.x, boneHips.position.y - sitHipsHeightThreshold, boneHips.position.z);
            Gizmos.DrawWireCube(sitThresholdPos, new Vector3(0.4f, 0.02f, 0.4f));
        }
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
    /// Memperbarui isi TextMeshPro dan visibilitas atau warna panel/font (Nyala vs Mati).
    /// </summary>
    public void UpdateUI()
    {
        EnsurePanelImages();

        if (useColorStatusInsteadOfPopups)
        {
            // SISTEM BARU: Panel SELALU MUNCUL (tidak perlu disembunyikan/pop-up)
            // Warna panel & font yang berubah menandakan status aktif/tidaknya.
            if (panelInfoPosisiJongkok != null && !panelInfoPosisiJongkok.activeSelf)
                panelInfoPosisiJongkok.SetActive(true);

            if (panelCounterJongkok != null && !panelCounterJongkok.activeSelf)
                panelCounterJongkok.SetActive(true);

            if (panelJarakTungkuJongkok != null && !panelJarakTungkuJongkok.activeSelf)
                panelJarakTungkuJongkok.SetActive(true);

            if (panelPosisiArahPlayer != null && !panelPosisiArahPlayer.activeSelf)
                panelPosisiArahPlayer.SetActive(true);

            // Hanya JONGKOK yang relevan untuk tracking tungku:
            // Panel jadi BIRU hanya jika Jongkok! Jika selain jongkok (Duduk Kursi, Duduk Selonjoran, Berdiri), panel MERAH.
            bool isBiru = (currentPostureType == PostureType.Squatting);

            // Update Warna Panel dan Font (Jika !isBiru, otomatis kembali ke MERAH dan font ABU)
            ApplyPanelColorState(panelInfoPosisiJongkok, imageInfoPosisiJongkok, textInfoPosisiJongkok, isBiru, colorPanelNyala);
            ApplyPanelColorState(panelCounterJongkok, imageCounterJongkok, textCounterJongkok, isBiru, colorPanelNyala);
            ApplyPanelColorState(panelJarakTungkuJongkok, imageJarakTungkuJongkok, textJarakTungkuJongkok, isBiru, colorPanelNyala);

            // Update Teks Label Posisi di panel:
            // Selalu tampilkan label postur yang aktif: Jongkok, Duduk Kursi, Duduk Selonjoran, atau Berdiri!
            if (textInfoPosisiJongkok != null)
            {
                string label = GetPostureLabelIndonesian();
                textInfoPosisiJongkok.text = $"{label} ({currentFacingCamera})";
            }

            if (textCounterJongkok != null)
            {
                textCounterJongkok.text = isBiru
                    ? formattedDuration
                    : textCounterMati;
            }

            if (textJarakTungkuJongkok != null)
            {
                textJarakTungkuJongkok.text = isBiru
                    ? (tungkuTransform != null ? $"{currentDistance:F2} m" : "-")
                    : textJarakMati;
            }
        }
        else
        {
            // SISTEM LAMA (Pop-up hide/show dengan SetActive)
            if (!manualControlPopups)
            {
                bool shouldShowJongkokPopups = forceShowAllPopups || !showPopupsOnlyWhenJongkok || isJongkok;

                if (panelInfoPosisiJongkok != null && panelInfoPosisiJongkok.activeSelf != shouldShowJongkokPopups)
                    panelInfoPosisiJongkok.SetActive(shouldShowJongkokPopups);

                if (panelCounterJongkok != null && panelCounterJongkok.activeSelf != shouldShowJongkokPopups)
                    panelCounterJongkok.SetActive(shouldShowJongkokPopups);

                if (panelJarakTungkuJongkok != null && panelJarakTungkuJongkok.activeSelf != shouldShowJongkokPopups)
                    panelJarakTungkuJongkok.SetActive(shouldShowJongkokPopups);

                if (panelPosisiArahPlayer != null && !panelPosisiArahPlayer.activeSelf)
                    panelPosisiArahPlayer.SetActive(true);
            }

            if (textInfoPosisiJongkok != null)
            {
                string label = GetPostureLabelIndonesian();
                textInfoPosisiJongkok.text = $"{label} ({currentFacingCamera})";
            }

            if (textCounterJongkok != null)
            {
                textCounterJongkok.text = formattedDuration;
            }

            if (textJarakTungkuJongkok != null)
            {
                textJarakTungkuJongkok.text = (tungkuTransform != null) ? $"{currentDistance:F2} m" : "-";
            }
        }

        if (textPosisiArahPlayer != null)
        {
            textPosisiArahPlayer.text = currentFacingCamera;
        }

        // Sinkronisasi visibilitas Tombol Buka Login:
        // Muncul saat panel tengah (panelLogin) disable, dan mati saat panel tengah enable
        EnsureLoginReferences();
        if (buttonBukaLogin != null && panelLogin != null)
        {
            bool shouldShowButton = !panelLogin.activeSelf;
            if (buttonBukaLogin.gameObject.activeSelf != shouldShowButton)
            {
                buttonBukaLogin.gameObject.SetActive(shouldShowButton);
            }
        }

        // LEGACY TRACKER PANEL (Jika ada)
        UpdateLegacyInfoText();
    }

    /// <summary>
    /// Membuka panel login (Panel Tengah).
    /// </summary>
    [ContextMenu("Buka Panel Login")]
    public void OpenLoginPanel()
    {
        EnsureLoginReferences();
        if (panelLogin != null)
        {
            panelLogin.SetActive(true);
            if (buttonBukaLogin != null) buttonBukaLogin.gameObject.SetActive(false);
            Debug.Log("[PlayerTungkuTracker] Panel Login berhasil dibuka.");
        }
        else
        {
            Debug.LogWarning("[PlayerTungkuTracker] Panel Login tidak ditemukan!");
        }
    }

    /// <summary>
    /// Menutup panel login (Panel Tengah).
    /// </summary>
    [ContextMenu("Tutup Panel Login")]
    public void CloseLoginPanel()
    {
        EnsureLoginReferences();
        if (panelLogin != null)
        {
            panelLogin.SetActive(false);
            if (buttonBukaLogin != null) buttonBukaLogin.gameObject.SetActive(true);
            Debug.Log("[PlayerTungkuTracker] Panel Login ditutup.");
        }
    }

    /// <summary>
    /// Toggle buka/tutup panel login (Panel Tengah).
    /// </summary>
    [ContextMenu("Toggle Panel Login")]
    public void ToggleLoginPanel()
    {
        EnsureLoginReferences();
        if (panelLogin != null)
        {
            bool nextState = !panelLogin.activeSelf;
            panelLogin.SetActive(nextState);
            if (buttonBukaLogin != null) buttonBukaLogin.gameObject.SetActive(!nextState);
            Debug.Log($"[PlayerTungkuTracker] Panel Login di-toggle ke: {nextState}");
        }
        else
        {
            Debug.LogWarning("[PlayerTungkuTracker] Panel Login tidak ditemukan!");
        }
    }

    private void EnsureLoginReferences()
    {
        if (panelLogin == null)
        {
            panelLogin = FindObjectInScene("Panel Tengah");
            if (panelLogin == null && KinectSessionManager.Instance != null)
            {
                panelLogin = KinectSessionManager.Instance.loginPanel;
            }
        }

        if (buttonBukaLogin == null)
        {
            GameObject panelKiriAtas = FindObjectInScene("Panel Kiri Atas");
            if (panelKiriAtas != null)
            {
                buttonBukaLogin = panelKiriAtas.GetComponentInChildren<Button>(true);
            }
            if (buttonBukaLogin == null)
            {
                GameObject btnObj = FindObjectInScene("Button");
                if (btnObj != null)
                {
                    buttonBukaLogin = btnObj.GetComponent<Button>();
                }
            }
        }
    }

    /// <summary>
    /// Mengaplikasikan warna background panel dan warna font (Nyala vs Mati).
    /// </summary>
    public void ApplyPanelColorState(GameObject panelGo, Image panelImg, TMP_Text mainText, bool isNyala, Color? customActiveColor = null)
    {
        if (panelImg == null && panelGo != null)
        {
            panelImg = panelGo.GetComponent<Image>();
        }

        if (panelImg != null)
        {
            Color activeColor = customActiveColor ?? colorPanelNyala;
            panelImg.color = isNyala ? activeColor : colorPanelMati;
        }

        Color targetFontColor = isNyala ? colorFontNyala : colorFontMati;

        if (mainText != null)
        {
            mainText.color = targetFontColor;
        }

        if (changeAllFontsInPanel && panelGo != null)
        {
            TMP_Text[] tmps = panelGo.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps)
            {
                t.color = targetFontColor;
            }
        }
    }

    public void EnsurePanelImages()
    {
        if (imageInfoPosisiJongkok == null && panelInfoPosisiJongkok != null)
            imageInfoPosisiJongkok = panelInfoPosisiJongkok.GetComponent<Image>();

        if (imageCounterJongkok == null && panelCounterJongkok != null)
            imageCounterJongkok = panelCounterJongkok.GetComponent<Image>();

        if (imageJarakTungkuJongkok == null && panelJarakTungkuJongkok != null)
            imageJarakTungkuJongkok = panelJarakTungkuJongkok.GetComponent<Image>();
    }

    /// <summary>
    /// Preview tampilan status Nyala (Biru / Cyan) atau Mati (Merah + Font Abu) langsung di Editor.
    /// <summary>
    /// Preview tampilan status postur (Jongkok = Biru, Duduk Kursi / Selonjoran / Berdiri = Merah) langsung di Editor.
    /// </summary>
    public void PreviewPostureInEditor(PostureType type)
    {
        AutoFindReferences();
        EnsurePanelImages();

        if (panelInfoPosisiJongkok != null) panelInfoPosisiJongkok.SetActive(true);
        if (panelCounterJongkok != null) panelCounterJongkok.SetActive(true);
        if (panelJarakTungkuJongkok != null) panelJarakTungkuJongkok.SetActive(true);
        if (panelPosisiArahPlayer != null) panelPosisiArahPlayer.SetActive(true);

        currentPostureType = type;
        currentPosture = type.ToString();
        isJongkok = (type == PostureType.Squatting);

        bool isBiru = isJongkok;
        ApplyPanelColorState(panelInfoPosisiJongkok, imageInfoPosisiJongkok, textInfoPosisiJongkok, isBiru, colorPanelNyala);
        ApplyPanelColorState(panelCounterJongkok, imageCounterJongkok, textCounterJongkok, isBiru, colorPanelNyala);
        ApplyPanelColorState(panelJarakTungkuJongkok, imageJarakTungkuJongkok, textJarakTungkuJongkok, isBiru, colorPanelNyala);

        if (textInfoPosisiJongkok != null)
        {
            string label = GetPostureLabelIndonesian();
            textInfoPosisiJongkok.text = $"{label} ({currentFacingCamera})";
        }

        if (textCounterJongkok != null)
            textCounterJongkok.text = isBiru ? "0:15" : textCounterMati;

        if (textJarakTungkuJongkok != null)
            textJarakTungkuJongkok.text = isBiru ? "1.85 m" : textJarakMati;

        if (textPosisiArahPlayer != null)
            textPosisiArahPlayer.text = "Menghadap Kamera";

#if UNITY_EDITOR
        MarkDirty(panelInfoPosisiJongkok);
        MarkDirty(panelCounterJongkok);
        MarkDirty(panelJarakTungkuJongkok);
        MarkDirty(textInfoPosisiJongkok);
        MarkDirty(textCounterJongkok);
        MarkDirty(textJarakTungkuJongkok);
#endif
    }

    /// <summary>
    /// Preview tampilan status Nyala (Biru) atau Mati (Merah + Font Abu) langsung di Editor.
    /// </summary>
    public void PreviewStatusInEditor(bool nyala, bool isDuduk = false)
    {
        PreviewPostureInEditor(nyala ? PostureType.Squatting : PostureType.Standing);
    }

    /// <summary>
    /// Membantu user menyalakan/mematikan seluruh panel pop-up di Scene saat mengedit UI di Editor.
    /// </summary>
    public void SetAllPopupsActiveInEditor(bool active)
    {
        if (panelInfoPosisiJongkok != null) panelInfoPosisiJongkok.SetActive(active);
        if (panelCounterJongkok != null) panelCounterJongkok.SetActive(active);
        if (panelJarakTungkuJongkok != null) panelJarakTungkuJongkok.SetActive(active);
        if (panelPosisiArahPlayer != null) panelPosisiArahPlayer.SetActive(active);

#if UNITY_EDITOR
        if (panelInfoPosisiJongkok != null) MarkDirty(panelInfoPosisiJongkok);
        if (panelCounterJongkok != null) MarkDirty(panelCounterJongkok);
        if (panelJarakTungkuJongkok != null) MarkDirty(panelJarakTungkuJongkok);
        if (panelPosisiArahPlayer != null) MarkDirty(panelPosisiArahPlayer);
#endif
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

        // Player Transform
        if (playerTransform == null)
        {
            AvatarController ac = FindObjectOfType<AvatarController>();
            if (ac != null) playerTransform = ac.transform;
            else
            {
                GameObject p = FindObjectInScene("Player") ?? FindObjectInScene("U_Character");
                if (p != null) playerTransform = p.transform;
            }
        }
        CachePlayerBones();

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

        // Panel Login ('Panel Tengah')
        if (panelLogin == null)
        {
            panelLogin = FindObjectInScene("Panel Tengah");
            if (panelLogin == null && KinectSessionManager.Instance != null)
            {
                panelLogin = KinectSessionManager.Instance.loginPanel;
            }
        }

        // Button Buka Login (di 'Panel Kiri Atas' atau dengan nama 'Button')
        if (buttonBukaLogin == null)
        {
            GameObject panelKiriAtas = FindObjectInScene("Panel Kiri Atas");
            if (panelKiriAtas != null)
            {
                buttonBukaLogin = panelKiriAtas.GetComponentInChildren<Button>(true);
            }
            if (buttonBukaLogin == null)
            {
                GameObject btnObj = FindObjectInScene("Button");
                if (btnObj != null)
                {
                    buttonBukaLogin = btnObj.GetComponent<Button>();
                }
            }
        }

        EnsurePanelImages();
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
        PreviewStatusInEditor(true);

        if (infoText != null)
        {
            infoText.text =
                "<b>Jarak ke Tungku:</b> <color=#00E5FF>1.85 m</color>\n" +
                "<b>Durasi Jongkok:</b> <color=#FFE082>0:15</color>\n" +
                "<b>Selisih (ΔXYZ):</b> <color=#FFE082>ΔX: +0.45m | ΔY: -0.10m | ΔZ: +1.79m</color>\n" +
                "<b>Hadap Kamera Kinect:</b> <color=#88FF88>Menghadap Kamera</color>\n" +
                "<b>Status Postur:</b> <color=#00E5FF>Jongkok / Duduk (Sitting)</color>";
#if UNITY_EDITOR
            MarkDirty(infoText);
#endif
        }

        Debug.Log("[PlayerTungkuTracker] Placeholder debug text & warna Nyala berhasil ditulis ke seluruh TMP Text!");
    }

#if UNITY_EDITOR
    private void MarkDirty(UnityEngine.Object obj)
    {
        if (obj == null) return;
        UnityEditor.EditorUtility.SetDirty(obj);
        if (obj is Component comp && comp.gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
        }
        else if (obj is GameObject go && go.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
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
        UnityEditor.EditorGUILayout.LabelField("Panel Login Controller", UnityEditor.EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
        if (GUILayout.Button("Buka / Toggle Panel Login", GUILayout.Height(30)))
        {
            tracker.ToggleLoginPanel();
            UnityEditor.EditorUtility.SetDirty(tracker);
        }

        UnityEditor.EditorGUILayout.Space(12);
        UnityEditor.EditorGUILayout.LabelField("Mode Otak-Atik Pop-Up (Manual)", UnityEditor.EditorStyles.boldLabel);

        GUI.backgroundColor = tracker.manualControlPopups ? new Color(1f, 0.55f, 0.2f) : new Color(0.7f, 0.9f, 0.7f);
        string manualBtnText = tracker.manualControlPopups
            ? "Mode Otak-Atik: [AKTIF] (Script TIDAK mematikan panel!)"
            : "Mode Otak-Atik: [NONAKTIF] (Script kontrol otomatis saat jongkok)";
        if (GUILayout.Button(manualBtnText, GUILayout.Height(32)))
        {
            tracker.manualControlPopups = !tracker.manualControlPopups;
            UnityEditor.EditorUtility.SetDirty(tracker);
        }

        GUI.backgroundColor = tracker.forceShowAllPopups ? new Color(1f, 0.85f, 0.2f) : new Color(0.85f, 0.85f, 0.85f);
        string forceBtnText = tracker.forceShowAllPopups
            ? "Paksa Semua Pop-Up Muncul di Play: [ON]"
            : "Paksa Semua Pop-Up Muncul di Play: [OFF]";
        if (GUILayout.Button(forceBtnText, GUILayout.Height(28)))
        {
            tracker.forceShowAllPopups = !tracker.forceShowAllPopups;
            tracker.UpdateUI();
            UnityEditor.EditorUtility.SetDirty(tracker);
        }

        UnityEditor.EditorGUILayout.Space(12);
        UnityEditor.EditorGUILayout.LabelField("Debug Tools & Preview Editor", UnityEditor.EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("Auto-Find References dari Scene Hierarchy", GUILayout.Height(30)))
        {
            tracker.AutoFindReferences();
            UnityEditor.EditorUtility.SetDirty(tracker);
        }

        GUI.backgroundColor = new Color(0.1f, 0.85f, 0.7f);
        if (GUILayout.Button("Snap Avatar ke Lantai Sekarang (Raycast Grounding)", GUILayout.Height(30)))
        {
            tracker.ApplyFloorGrounding();
            if (tracker.playerTransform != null)
            {
                UnityEditor.EditorUtility.SetDirty(tracker.playerTransform);
            }
            UnityEditor.EditorUtility.SetDirty(tracker);
        }

        GUI.backgroundColor = new Color(0.85f, 0.75f, 0.3f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable Semua Pop-Up (Edit UI)", GUILayout.Height(28)))
        {
            tracker.SetAllPopupsActiveInEditor(true);
        }
        if (GUILayout.Button("Disable Semua Pop-Up", GUILayout.Height(28)))
        {
            tracker.SetAllPopupsActiveInEditor(false);
        }
        GUILayout.EndHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
        if (GUILayout.Button("Tulis Debug Placeholder Text ke TMP", GUILayout.Height(30)))
        {
            tracker.SetDebugPlaceholderText();
        }

        UnityEditor.EditorGUILayout.Space(8);
        UnityEditor.EditorGUILayout.LabelField("Preview Status Postur & Warna Panel (Editor)", UnityEditor.EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.15f, 0.45f, 0.95f);
        if (GUILayout.Button("Preview JONGKOK\n(Biru - Relevan)", GUILayout.Height(38)))
        {
            tracker.PreviewPostureInEditor(PostureType.Squatting);
            UnityEditor.EditorUtility.SetDirty(tracker);
        }
        GUI.backgroundColor = new Color(0.95f, 0.35f, 0.35f);
        if (GUILayout.Button("Preview DUDUK KURSI\n(Merah + Abu)", GUILayout.Height(38)))
        {
            tracker.PreviewPostureInEditor(PostureType.SittingChair);
            UnityEditor.EditorUtility.SetDirty(tracker);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.95f, 0.35f, 0.35f);
        if (GUILayout.Button("Preview SELONJORAN\n(Merah + Abu)", GUILayout.Height(38)))
        {
            tracker.PreviewPostureInEditor(PostureType.SittingFloor);
            UnityEditor.EditorUtility.SetDirty(tracker);
        }
        if (GUILayout.Button("Preview BERDIRI\n(Merah + Abu)", GUILayout.Height(38)))
        {
            tracker.PreviewPostureInEditor(PostureType.Standing);
            UnityEditor.EditorUtility.SetDirty(tracker);
        }
        GUILayout.EndHorizontal();

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
