using System;

using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif



/// <summary>

/// PoseCalibrationManager

/// Sistem manajemen kalibrasi, showcase pose 100%, perekam nilai pose acuan,

/// dan penghitung akurasi persentase (%) untuk setiap label postur.

/// Menggunakan referensi GameObject terpisah (dummy avatar) sehingga avatar asli tidak terganggu.

/// </summary>

[ExecuteAlways]

[DisallowMultipleComponent]

public class PoseCalibrationManager : MonoBehaviour

{

    [Header("1. Dummy / Reference Avatar (Terpisah dari Avatar Asli)")]

    [Tooltip("Tarik GameObject avatar dummy/referensi ke sini untuk otak-atik tanpa menyentuh avatar asli.")]

    public GameObject referencePoseAvatar;



    [Header("2. Referensi Live Tracker (Opsional)")]

    [Tooltip("Referensi ke PlayerTungkuTracker. Jika kosong, otomatis mencari di scene.")]

    public PlayerTungkuTracker tungkuTracker;



    [Header("3. Status Label & Akurasi Realtime")]

    [Tooltip("Label postur terbaik yang sedang terdeteksi saat ini.")]

    [SerializeField] private PostureType detectedPosture = PostureType.Standing;

    [SerializeField] private string detectedPostureLabel = "Berdiri";

    [SerializeField] private float topAccuracyPercent = 100f;



    [Header("3a. Filter Label Aktif (Enable / Disable Postur)")]
    [Tooltip("Aktifkan evaluasi label postur Berdiri.")]
    public bool enableStanding = true;
    public bool enableBerdiri { get => enableStanding; set => enableStanding = value; }

    [Tooltip("Aktifkan evaluasi label postur Jongkok.")]
    public bool enableSquatting = true;
    public bool enableJongkok { get => enableSquatting; set => enableSquatting = value; }

    [Tooltip("Aktifkan evaluasi label postur Berlutut.")]
    public bool enableKneeling = true;
    public bool enableBerlutut { get => enableKneeling; set => enableKneeling = value; }

    [Tooltip("Aktifkan evaluasi label postur Duduk Sila.")]
    public bool enableSittingCrossLegged = true;
    public bool enableDudukSila { get => enableSittingCrossLegged; set => enableSittingCrossLegged = value; }

    [Tooltip("Aktifkan evaluasi label postur Duduk Emok.")]
    public bool enableSittingEmok = true;
    public bool enableDudukEmok { get => enableSittingEmok; set => enableSittingEmok = value; }

    [Tooltip("Aktifkan evaluasi label postur Duduk Normal.")]
    public bool enableSittingChair = true;
    public bool enableDudukNormal { get => enableSittingChair; set => enableSittingChair = value; }
    public bool enableDudukKursi { get => enableSittingChair; set => enableSittingChair = value; }

    public bool IsPostureEnabled(PostureType posture)

    {

        switch (posture)

        {

            case PostureType.Standing: return enableStanding;

            case PostureType.Squatting: return enableSquatting;

            case PostureType.Kneeling: return enableKneeling;

            case PostureType.SittingCrossLegged: return enableSittingCrossLegged;

            case PostureType.SittingEmok: return enableSittingEmok;

            case PostureType.SittingChair: return enableSittingChair;

            default: return true;

        }

    }



    public void SetPostureEnabled(PostureType posture, bool enabled)

    {

        switch (posture)

        {

            case PostureType.Standing: enableStanding = enabled; break;

            case PostureType.Squatting: enableSquatting = enabled; break;

            case PostureType.Kneeling: enableKneeling = enabled; break;

            case PostureType.SittingCrossLegged: enableSittingCrossLegged = enabled; break;

            case PostureType.SittingEmok: enableSittingEmok = enabled; break;

            case PostureType.SittingChair: enableSittingChair = enabled; break;

        }

    }



    public void EnableAllPostures(bool enabled)

    {

        enableStanding = enabled;

        enableSquatting = enabled;

        enableKneeling = enabled;

        enableSittingCrossLegged = enabled;

        enableSittingEmok = enabled;

        enableSittingChair = enabled;

    }



    public void EnableOnlySittingChair()

    {

        enableStanding = true;

        enableSquatting = true;

        enableKneeling = false;

        enableSittingCrossLegged = false;

        enableSittingEmok = false;

        enableSittingChair = true;

    }



    public void DisableAllSitting()

    {

        enableStanding = true;

        enableSquatting = true;

        enableKneeling = true;

        enableSittingCrossLegged = false;

        enableSittingEmok = false;

        enableSittingChair = false;

    }



    [Header("3b. Status Deteksi Boolean (Realtime Checkbox)")]

    [Tooltip("Apakah postur BERDIRI terdeteksi?")]

    public bool isBerdiri = true;

    [Tooltip("Apakah postur JONGKOK terdeteksi?")]

    public bool isJongkok = false;

    [Tooltip("Apakah postur BERLUTUT terdeteksi?")]

    public bool isBerlutut = false;

    [Tooltip("Apakah postur DUDUK SILA terdeteksi?")]

    public bool isDudukSila = false;

    [Tooltip("Apakah postur DUDUK EMOK terdeteksi?")]

    public bool isDudukEmok = false;

    [Tooltip("Apakah postur DUDUK NORMAL terdeteksi?")]

    [UnityEngine.Serialization.FormerlySerializedAs("isDudukKursi")]

    public bool isDudukNormal = false;

    public bool isDudukKursi { get => isDudukNormal; set => isDudukNormal = value; }



    [System.Serializable]

    public class PostureScore

    {

        public PostureType postureType;

        public string labelIndonesian;

        public bool isEnabled;

        [Range(0f, 100f)] public float accuracyPercent;

        public bool isDetectedWinner;



        public PostureScore(PostureType type, string label, bool enabled, float accuracy, bool winner)

        {

            postureType = type;

            labelIndonesian = label;

            isEnabled = enabled;

            accuracyPercent = accuracy;

            isDetectedWinner = winner;

        }



        public PostureScore(PostureType type, string label, float accuracy, bool winner)

        {

            postureType = type;

            labelIndonesian = label;

            isEnabled = true;

            accuracyPercent = accuracy;

            isDetectedWinner = winner;

        }

    }



    [Header("4. Tabel Akurasi Semua Label (%)")]

    [Tooltip("Daftar persentase kecocokan terhadap seluruh label yang tersedia.")]

    public List<PostureScore> allPostureScores = new List<PostureScore>();



    [Header("5. Live Metrics Monitor (Dummy Avatar)")]

    [SerializeField] private float liveHipHeight = 1.065582f;

    [SerializeField] private float liveKneeAngle = 180f;

    [SerializeField] private float liveKneeHeight = 0.541899f;

    [SerializeField] private float liveKneeSpread = 0.136703f;

    [SerializeField] private float liveAnkleAsymmetry = 0f;

    [SerializeField] private float liveHipToFootDistance = 0.928137f;



    [Header("6. Profil Ambang Batas Acuan (Calibrated Thresholds)")]

    [Tooltip("Tinggi pinggul acuan untuk Jongkok (meter).")]

    [Range(0.05f, 1.20f)] public float squatHipHeight = 0.48f;

    [Range(5f, 175f)] public float squatKneeAngle = 65f;



    [Tooltip("Parameter acuan untuk Berlutut.")]

    [Range(0.0f, 0.80f)] public float kneelingKneeHeight = 0.08f;

    [Range(0.05f, 1.20f)] public float kneelingHipHeight = 0.58f;



    [Tooltip("Parameter acuan untuk Duduk Sila (Cross-Legged).")]

    [Range(0.02f, 1.00f)] public float silaHipHeight = 0.22f;

    [Range(0.05f, 1.20f)] public float silaKneeSpread = 0.60f;



    [Tooltip("Parameter acuan untuk Duduk Emok (Side-Folded).")]

    [Range(0.02f, 1.00f)] public float emokHipHeight = 0.22f;

    [Range(0.01f, 1.00f)] public float emokAnkleAsymmetry = 0.16f;



    [Tooltip("Parameter acuan untuk Duduk Normal.")]

    [Range(0.10f, 1.20f)] public float chairHipHeight = 0.70f;

    [Range(10f, 175f)] public float chairKneeAngle = 95f;



    [System.Serializable]

    public class SavedBoneTransform

    {

        public string boneName;

        public Vector3 localPosition;

        public Quaternion localRotation;

    }



    [System.Serializable]

    public class RecordedPostureSnapshot

    {

        public PostureType postureType;

        public bool hasCustomRecording;

        public float recordedHipHeight;

        public float recordedKneeAngle;

        public float recordedKneeHeight;

        public float recordedKneeSpread;

        public float recordedAnkleAsymmetry;

        public List<SavedBoneTransform> boneTransforms = new List<SavedBoneTransform>();

    }



    [Header("6b. Data Snapshot Pose Kustom (Hasil Rekam Sendi)")]

    public List<RecordedPostureSnapshot> customRecordedPoses = new List<RecordedPostureSnapshot>();



    [Header("7. Pengaturan On-Screen GUI")]

    [Tooltip("Tampilkan panel showcase & akurasi di layar saat Play mode.")]

    public bool showOnScreenGUI = true;

    [Tooltip("Hotkey keyboard untuk membuka/menutup panel GUI.")]

    public KeyCode toggleGuiHotkey = KeyCode.F3;



    // Public Getters

    public PostureType DetectedPosture => detectedPosture;

    public string DetectedPostureLabel => detectedPostureLabel;

    public float TopAccuracyPercent => topAccuracyPercent;

    public float LiveHipHeight => liveHipHeight;

    public float LiveKneeAngle => liveKneeAngle;

    public float LiveKneeHeight => liveKneeHeight;

    public float LiveKneeSpread => liveKneeSpread;

    public float LiveAnkleAsymmetry => liveAnkleAsymmetry;

    public bool IsShowcaseMode => isShowcaseMode;

    public PostureType ActiveShowcasePose => activeShowcasePose;



    // Cache tulang dummy

    public Animator dummyAnimator;

    public Transform boneHips;

    public Transform boneHipMaster;

    public Transform boneLeftThigh;

    public Transform boneRightThigh;

    public Transform boneLeftKnee;

    public Transform boneRightKnee;

    public Transform boneLeftFoot;

    public Transform boneRightFoot;

    public Transform boneLeftToe;

    public Transform boneRightToe;



    // Backup T-Pose default (Serialized agar tidak hilang saat domain reload / compile)

    [SerializeField] private bool hasOriginalPoseBackup = false;

    [SerializeField] private Vector3 originalHipsLocalPos = new Vector3(0f, 1.06558204f, 0.0124995559f);

    [SerializeField] private Quaternion originalHipsLocalRot = new Quaternion(0.00414705975f, -0.70709461f, -0.00414705975f, 0.70709461f);

    [SerializeField] private Vector3 originalLThighPos = new Vector3(-0.0683515444f, 0.075081639f, 0.0123760328f);

    [SerializeField] private Quaternion originalLThighRot = new Quaternion(0.014793925f, -0.70695436f, -0.0146810114f, 0.706952035f);

    [SerializeField] private Vector3 originalRThighPos = new Vector3(0.0683514029f, 0.0750816837f, 0.0123760402f);

    [SerializeField] private Quaternion originalRThighRot = new Quaternion(-0.706952035f, -0.0146810031f, 0.70695436f, 0.0147939119f);

    [SerializeField] private Vector3 originalLKneePos = new Vector3(0f, 0.438601226f, 0f);

    [SerializeField] private Quaternion originalLKneeRot = new Quaternion(-0.000709471235f, 1.42422195e-05f, -0.016383836f, 0.999865532f);

    [SerializeField] private Vector3 originalRKneePos = new Vector3(0f, -0.438601345f, 0f);

    [SerializeField] private Quaternion originalRKneeRot = new Quaternion(-0.000709556916f, 9.01003568e-06f, -0.0163838509f, 0.999865532f);

    [SerializeField] private Vector3 originalLFootPos = new Vector3(0f, 0.404453605f, 0f);

    [SerializeField] private Quaternion originalLFootRot = new Quaternion(0.000628483656f, 0f, 0.0372194164f, 0.999306917f);

    [SerializeField] private Vector3 originalRFootPos = new Vector3(0f, -0.404453635f, 0f);

    [SerializeField] private Quaternion originalRFootRot = new Quaternion(0.000628483482f, 0f, 0.0372194126f, 0.999306917f);

    [SerializeField] private float standingHipHeight = 1.065582f;

    [SerializeField] private PostureType activeShowcasePose = PostureType.Standing;

    [SerializeField] private bool isShowcaseMode = false;



    // Data kanonik Bind Pose murni untuk rig U_Character_REF.fbx

    private struct BoneBindData

    {

        public Vector3 localPos;

        public Quaternion localRot;



        public BoneBindData(Vector3 pos, Quaternion rot)

        {

            localPos = pos;

            localRot = rot;

        }

    }



    private static readonly Dictionary<string, BoneBindData> UCharacterBindPoseMap = new Dictionary<string, BoneBindData>(StringComparer.OrdinalIgnoreCase)

    {

        { "U", new BoneBindData(Vector3.zero, Quaternion.identity) },

        { "joint_Char", new BoneBindData(Vector3.zero, Quaternion.identity) },

        { "joint_Pelvis", new BoneBindData(new Vector3(0f, 1.06558204f, 0.0124995559f), new Quaternion(0.00414705975f, -0.70709461f, -0.00414705975f, 0.70709461f)) },

        { "joint_HipMaster", new BoneBindData(new Vector3(0.000117294541f, -0.00999931153f, 0f), new Quaternion(0.70709461f, 0.00414705975f, -0.70709461f, 0.00414705975f)) },

        { "joint_HipLT", new BoneBindData(new Vector3(-0.0683515444f, 0.075081639f, 0.0123760328f), new Quaternion(0.014793925f, -0.70695436f, -0.0146810114f, 0.706952035f)) },

        { "joint_KneeLT", new BoneBindData(new Vector3(0f, 0.438601226f, 0f), new Quaternion(-0.000709471235f, 1.42422195e-05f, -0.016383836f, 0.999865532f)) },

        { "joint_FootLT", new BoneBindData(new Vector3(0f, 0.404453605f, 0f), new Quaternion(0.000628483656f, 0f, 0.0372194164f, 0.999306917f)) },

        { "joint_ToeLT", new BoneBindData(new Vector3(-0.130712837f, 0.0870534405f, 0.00049193576f), new Quaternion(-0.00941892341f, -0.00651530456f, 0.568252087f, 0.822774827f)) },

        { "joint_HipRT", new BoneBindData(new Vector3(0.0683514029f, 0.0750816837f, 0.0123760402f), new Quaternion(-0.706952035f, -0.0146810031f, 0.70695436f, 0.0147939119f)) },

        { "joint_KneeRT", new BoneBindData(new Vector3(0f, -0.438601345f, 0f), new Quaternion(-0.000709556916f, 9.01003568e-06f, -0.0163838509f, 0.999865532f)) },

        { "joint_FootRT", new BoneBindData(new Vector3(0f, -0.404453635f, 0f), new Quaternion(0.000628483482f, 0f, 0.0372194126f, 0.999306917f)) },

        { "joint_ToeRT", new BoneBindData(new Vector3(0.130712882f, -0.0870534107f, -0.000491935585f), new Quaternion(-0.00941892061f, -0.00651530502f, 0.568252265f, 0.822774768f)) },

        { "joint_TorsoA", new BoneBindData(new Vector3(-0.000117294541f, 0.00999931153f, 0f), new Quaternion(0f, 0f, -0.0359426811f, 0.999353886f)) },

        { "joint_TorsoB", new BoneBindData(new Vector3(0f, 0.10734129f, 0f), new Quaternion(0f, 0f, 0.0652137771f, 0.997871339f)) },

        { "joint_TorsoC", new BoneBindData(new Vector3(0f, 0.112310328f, 0f), Quaternion.identity) },

        { "joint_ClavicleLT", new BoneBindData(new Vector3(-0.025573561f, 0.11283838f, 0.0365329161f), new Quaternion(0.00590039557f, 0.708494782f, 0.705163479f, 0.0272915792f)) },

        { "joint_ShoulderLT", new BoneBindData(new Vector3(0f, 0.0935312361f, 0f), new Quaternion(-0.0169570241f, -0.000188299877f, 0.0145302135f, 0.999750614f)) },

        { "joint_ElbowLT", new BoneBindData(new Vector3(0f, 0.226996094f, 0f), new Quaternion(2.55187319e-06f, -0.00289045484f, 0.00088933314f, 0.99999547f)) },

        { "joint_HandLT", new BoneBindData(new Vector3(0f, 0.233139858f, 0f), Quaternion.identity) },

        { "joint_CapitateLT", new BoneBindData(new Vector3(-0.0214241017f, 0.0697902963f, -0.00247824588f), new Quaternion(-0.116362035f, 0.694368005f, 0.0935398564f, 0.703962624f)) },

        { "joint_ClavicleRT", new BoneBindData(new Vector3(-0.02557371f, 0.112838455f, -0.0365330242f), new Quaternion(0.708495021f, -0.00590071781f, -0.0272912625f, 0.70516324f)) },

        { "joint_ShoulderRT", new BoneBindData(new Vector3(0f, -0.0935311764f, 0f), new Quaternion(-0.0169573575f, -0.000188289181f, 0.0145297619f, 0.999750614f)) },

        { "joint_ElbowRT", new BoneBindData(new Vector3(0f, -0.226996049f, 0f), new Quaternion(2.55189639e-06f, -0.00289045507f, 0.000889341289f, 0.99999547f)) },

        { "joint_HandRT", new BoneBindData(new Vector3(0f, -0.233139992f, 0f), Quaternion.identity) },

        { "joint_CapitateRT", new BoneBindData(new Vector3(0.0214241166f, -0.0697903261f, 0.00247824565f), new Quaternion(-0.116362058f, 0.694367945f, 0.0935398936f, 0.703962564f)) }

    };



    void Awake()

    {

        FindReferences();

        DisableDummyAvatarControllers();

        CacheDummyBones();

    }



    void OnEnable()

    {

        FindReferences();

        DisableDummyAvatarControllers();

        CacheDummyBones();

        UpdateLiveMetrics();



        // Saat domain reload di Editor atau scene dibuka: jika pose Standing default (belum direkam kustom) tapi lutut/pinggul masih menekuk, luruskan otomatis

        if (referencePoseAvatar != null && activeShowcasePose == PostureType.Standing && !HasRecordedPose(PostureType.Standing) && (liveKneeAngle < 172f || liveHipHeight < 0.85f))

        {

            ShowcasePose(PostureType.Standing);

        }

        else

        {

            RefreshDummyVisualizer();

            EvaluateAllScores();

        }

#if UNITY_EDITOR

        SceneView.RepaintAll();

#endif

    }



    void Start()

    {

        FindReferences();

        DisableDummyAvatarControllers();

        CacheDummyBones();

        UpdateLiveMetrics();



        // Jika pose aktif adalah Standing default tapi tulang dummy masih menekuk dari sesi sebelumnya, luruskan otomatis

        if (referencePoseAvatar != null && activeShowcasePose == PostureType.Standing && !HasRecordedPose(PostureType.Standing) && (liveKneeAngle < 172f || liveHipHeight < 0.85f))

        {

            ShowcasePose(PostureType.Standing);

        }

        else

        {

            RefreshDummyVisualizer();

            EvaluateAllScores();

        }

    }



    void OnValidate()

    {

        DisableDummyAvatarControllers();

        if (referencePoseAvatar != null && boneHips == null)

        {

            CacheDummyBones();

        }

    }



    void Update()

    {

        if (Application.isPlaying && Input.GetKeyDown(toggleGuiHotkey))

        {

            showOnScreenGUI = !showOnScreenGUI;

        }



        if (referencePoseAvatar != null)

        {

            DisableDummyAvatarControllers();

            UpdateLiveMetrics();

            EvaluateAllScores();

        }

    }



    private void FindReferences()

    {

        if (tungkuTracker == null)

        {

            tungkuTracker = FindObjectOfType<PlayerTungkuTracker>();

        }

    }



    /// <summary>

    /// Memastikan komponen AvatarController pada dummy avatar selalu nonaktif (enabled = false)

    /// agar tidak menimpa rotasi/posisi tulang dummy di background.

    /// </summary>

    public void DisableDummyAvatarControllers()

    {

        if (referencePoseAvatar == null) return;

        AvatarController[] controllers = referencePoseAvatar.GetComponentsInChildren<AvatarController>(true);

        for (int i = 0; i < controllers.Length; i++)

        {

            if (controllers[i] != null && controllers[i].enabled)

            {

                controllers[i].enabled = false;

            }

        }

    }



    /// <summary>

    /// Memperbarui visual garis & titik hijau (JointSphereVisualizer) secara instan di Scene View.

    /// </summary>

    public void RefreshDummyVisualizer()

    {

        if (referencePoseAvatar == null) return;

        JointSphereVisualizer[] visualizers = referencePoseAvatar.GetComponentsInChildren<JointSphereVisualizer>(true);

        for (int i = 0; i < visualizers.Length; i++)

        {

            if (visualizers[i] != null)

            {

                visualizers[i].UpdateVisualizerImmediate();

            }

        }

    }



    public void CacheDummyBones()

    {

        if (referencePoseAvatar == null) return;



        DisableDummyAvatarControllers();



        // 1. Pencarian spesifik untuk hierarki U_Character_REF / rig standar terlebih dahulu

        Transform root = referencePoseAvatar.transform;

        if (boneHips == null) boneHips = FindBoneExactOrKeyword(root, "joint_Pelvis", "joint_pelvis", "pelvis", "hips");

        if (boneHipMaster == null) boneHipMaster = FindBoneExactOrKeyword(root, "joint_HipMaster", "joint_hipmaster", "hipmaster");

        if (boneLeftThigh == null) boneLeftThigh = FindBoneExactOrKeyword(root, "joint_HipLT", "joint_hiplt", "hiplt", "leftupperleg", "leftthigh", "thigh.l", "thigh_l", "upleg.l");

        if (boneRightThigh == null) boneRightThigh = FindBoneExactOrKeyword(root, "joint_HipRT", "joint_hiprt", "hiprt", "rightupperleg", "rightthigh", "thigh.r", "thigh_r", "upleg.r");

        if (boneLeftKnee == null) boneLeftKnee = FindBoneExactOrKeyword(root, "joint_KneeLT", "joint_kneelt", "kneelt", "leftlowerleg", "leftknee", "knee.l", "knee_l", "shin.l");

        if (boneRightKnee == null) boneRightKnee = FindBoneExactOrKeyword(root, "joint_KneeRT", "joint_kneert", "kneert", "rightlowerleg", "rightknee", "knee.r", "knee_r", "shin.r");

        if (boneLeftFoot == null) boneLeftFoot = FindBoneExactOrKeyword(root, "joint_FootLT", "joint_footlt", "footlt", "anklelt", "leftfoot", "foot.l", "foot_l", "ankle.l");

        if (boneRightFoot == null) boneRightFoot = FindBoneExactOrKeyword(root, "joint_FootRT", "joint_footrt", "footrt", "anklert", "rightfoot", "foot.r", "foot_r", "ankle.r");

        if (boneLeftToe == null) boneLeftToe = FindBoneExactOrKeyword(root, "joint_ToeLT", "joint_toelt", "toelt", "lefttoes", "toe.l", "toe_l");

        if (boneRightToe == null) boneRightToe = FindBoneExactOrKeyword(root, "joint_ToeRT", "joint_toert", "toert", "righttoes", "toe.r", "toe_r");



        // 2. Fallback dari Mecanim Animator jika masih ada yang kosong

        if (dummyAnimator == null) dummyAnimator = referencePoseAvatar.GetComponentInChildren<Animator>();

        if (dummyAnimator != null && dummyAnimator.isHuman)

        {

            if (boneHips == null) boneHips = dummyAnimator.GetBoneTransform(HumanBodyBones.Hips);

            if (boneLeftThigh == null) boneLeftThigh = dummyAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);

            if (boneRightThigh == null) boneRightThigh = dummyAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);

            if (boneLeftKnee == null) boneLeftKnee = dummyAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);

            if (boneRightKnee == null) boneRightKnee = dummyAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg);

            if (boneLeftFoot == null) boneLeftFoot = dummyAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);

            if (boneRightFoot == null) boneRightFoot = dummyAnimator.GetBoneTransform(HumanBodyBones.RightFoot);

            if (boneLeftToe == null) boneLeftToe = dummyAnimator.GetBoneTransform(HumanBodyBones.LeftToes);

            if (boneRightToe == null) boneRightToe = dummyAnimator.GetBoneTransform(HumanBodyBones.RightToes);

        }



        // 3. Fallback dari AvatarController jika ada

        AvatarController ac = referencePoseAvatar.GetComponent<AvatarController>() ?? referencePoseAvatar.GetComponentInChildren<AvatarController>(true);

        if (ac != null)

        {

            int hipIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.SpineBase, false);

            int thighLIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.HipLeft, false);

            int thighRIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.HipRight, false);

            int kneeLIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.KneeLeft, false);

            int kneeRIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.KneeRight, false);

            int footLIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.FootLeft, false);

            int footRIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.FootRight, false);

            int ankleLIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.AnkleLeft, false);

            int ankleRIdx = ac.GetBoneIndexByJoint(KinectInterop.JointType.AnkleRight, false);



            if (boneHips == null && hipIdx >= 0) boneHips = ac.GetBoneTransform(hipIdx);

            if (boneLeftThigh == null && thighLIdx >= 0) boneLeftThigh = ac.GetBoneTransform(thighLIdx);

            if (boneRightThigh == null && thighRIdx >= 0) boneRightThigh = ac.GetBoneTransform(thighRIdx);

            if (boneLeftKnee == null && kneeLIdx >= 0) boneLeftKnee = ac.GetBoneTransform(kneeLIdx);

            if (boneRightKnee == null && kneeRIdx >= 0) boneRightKnee = ac.GetBoneTransform(kneeRIdx);

            if (boneLeftFoot == null) boneLeftFoot = (footLIdx >= 0 ? ac.GetBoneTransform(footLIdx) : null) ?? (ankleLIdx >= 0 ? ac.GetBoneTransform(ankleLIdx) : null);

            if (boneRightFoot == null) boneRightFoot = (footRIdx >= 0 ? ac.GetBoneTransform(footRIdx) : null) ?? (ankleRIdx >= 0 ? ac.GetBoneTransform(ankleRIdx) : null);

        }



        BackupOriginalPose();

    }



    /// <summary>

    /// Menyimpan backup bind pose murni. Jika tulang merupakan rig U_Character_REF atau Prefab FBX,

    /// selalu gunakan nilai bind pose murni FBX agar backup tidak pernah terkontaminasi pose jongkok/emok.

    /// </summary>

    public void BackupOriginalPose()

    {

        if (boneHips == null) return;



        // Jika backup belum ada atau terdeteksi terkontaminasi pose rendah (misal hip Y < 0.85m), ambil dari bind pose murni

        if (!hasOriginalPoseBackup || originalHipsLocalPos.y < 0.85f || standingHipHeight < 0.85f)

        {

            ReadPureBindPoseForBone(boneHips, out originalHipsLocalPos, out originalHipsLocalRot);

            if (boneLeftThigh != null) ReadPureBindPoseForBone(boneLeftThigh, out originalLThighPos, out originalLThighRot);

            if (boneRightThigh != null) ReadPureBindPoseForBone(boneRightThigh, out originalRThighPos, out originalRThighRot);

            if (boneLeftKnee != null) ReadPureBindPoseForBone(boneLeftKnee, out originalLKneePos, out originalLKneeRot);

            if (boneRightKnee != null) ReadPureBindPoseForBone(boneRightKnee, out originalRKneePos, out originalRKneeRot);

            if (boneLeftFoot != null) ReadPureBindPoseForBone(boneLeftFoot, out originalLFootPos, out originalLFootRot);

            if (boneRightFoot != null) ReadPureBindPoseForBone(boneRightFoot, out originalRFootPos, out originalRFootRot);



            standingHipHeight = Mathf.Max(0.85f, originalHipsLocalPos.y);

            hasOriginalPoseBackup = true;

        }

    }



    private bool ReadPureBindPoseForBone(Transform bone, out Vector3 bindPos, out Quaternion bindRot)

    {

        bindPos = bone != null ? bone.localPosition : Vector3.zero;

        bindRot = bone != null ? bone.localRotation : Quaternion.identity;

        if (bone == null) return false;



#if UNITY_EDITOR

        Transform prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(bone);

        if (prefabSource != null)

        {

            bindPos = prefabSource.localPosition;

            bindRot = prefabSource.localRotation;

            return true;

        }

#endif

        if (UCharacterBindPoseMap.TryGetValue(bone.name, out BoneBindData data))

        {

            bindPos = data.localPos;

            bindRot = data.localRot;

            return true;

        }



        return false;

    }



    /// <summary>

    /// Mengembalikan seluruh hierarki tulang dummy avatar ke T-Pose murni bawaan file FBX

    /// tanpa mengubah posisi/rotasi root avatar di scene (tidak teleport ke 0,0,0).

    /// </summary>

    public void ForceResetDummyToPureTPose()

    {

        if (referencePoseAvatar == null) return;



        Vector3 rootWorldPos = referencePoseAvatar.transform.position;

        Quaternion rootWorldRot = referencePoseAvatar.transform.rotation;

        Vector3 rootLocalScale = referencePoseAvatar.transform.localScale;



        // 1. Matikan AvatarController di dummy agar tidak menimpa posisi/rotasi

        DisableDummyAvatarControllers();



        // 2. Kembalikan seluruh tulang anak ke bind pose murni FBX (tanpa menyentuh root transform)

        Transform[] allTransforms = referencePoseAvatar.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < allTransforms.Length; i++)

        {

            Transform t = allTransforms[i];

            if (t == null || t == referencePoseAvatar.transform) continue;

            if (t.name.StartsWith("VisJoint_") || t.name.StartsWith("Line_") || t.name == "VisLines_Holder") continue;



#if UNITY_EDITOR

            Transform source = PrefabUtility.GetCorrespondingObjectFromSource(t);

            if (source != null)

            {

                t.localPosition = source.localPosition;

                t.localRotation = source.localRotation;

                t.localScale = source.localScale;

                continue;

            }

#endif

            if (UCharacterBindPoseMap.TryGetValue(t.name, out BoneBindData bindData))

            {

                t.localPosition = bindData.localPos;

                t.localRotation = bindData.localRot;

                t.localScale = Vector3.one;

            }

        }



        // 3. Kunci posisi root avatar agar tidak teleport ke (0,0,0)

        referencePoseAvatar.transform.position = rootWorldPos;

        referencePoseAvatar.transform.rotation = rootWorldRot;

        referencePoseAvatar.transform.localScale = rootLocalScale;



        // 4. Perbarui cache sendi tulang dan simpan nilai T-Pose murni ke backup permanen

        hasOriginalPoseBackup = false;

        CacheDummyBones();



        // 5. Pastikan panggul, paha, lutut, dan telapak kaki benar-benar lurus vertikal sempurna

        ApplyStraightStandingLegPose();



        isShowcaseMode = true;

        activeShowcasePose = PostureType.Standing;



        RefreshDummyVisualizer();

        UpdateLiveMetrics();

        EvaluateAllScores();

        Debug.Log("[PoseCalibrationManager] Dummy avatar berhasil di-reset 100% ke T-Pose / Berdiri tegak murni FBX!");

    }



    /// <summary>

    /// Memastikan panggul tegak penuh (tanpa offset minus Y), paha kiri & kanan vertikal simetris,

    /// lutut lurus 180 derajat (0 derajat tekukan), dan telapak kaki rata sejajar di lantai.

    /// </summary>

    private void ApplyStraightStandingLegPose()

    {

        if (boneHips != null)

        {

            boneHips.localPosition = originalHipsLocalPos;

            boneHips.localRotation = originalHipsLocalRot;

        }



        if (boneHipMaster != null && UCharacterBindPoseMap.TryGetValue(boneHipMaster.name, out BoneBindData hmBind))

        {

            boneHipMaster.localPosition = hmBind.localPos;

            boneHipMaster.localRotation = hmBind.localRot;

        }



        if (boneLeftThigh != null)

        {

            boneLeftThigh.localPosition = originalLThighPos;

            // Orientasi vertikal murni untuk joint_HipLT pada rig U_Character_REF

            if (boneLeftThigh.name.Equals("joint_HipLT", StringComparison.OrdinalIgnoreCase))

                boneLeftThigh.localRotation = new Quaternion(0f, -0.7071068f, 0f, 0.7071068f);

            else

                boneLeftThigh.localRotation = originalLThighRot;

        }



        if (boneRightThigh != null)

        {

            boneRightThigh.localPosition = originalRThighPos;

            // Orientasi vertikal murni untuk joint_HipRT pada rig U_Character_REF

            if (boneRightThigh.name.Equals("joint_HipRT", StringComparison.OrdinalIgnoreCase))

                boneRightThigh.localRotation = new Quaternion(-0.7071068f, 0f, 0.7071068f, 0f);

            else

                boneRightThigh.localRotation = originalRThighRot;

        }



        if (boneLeftKnee != null)

        {

            boneLeftKnee.localPosition = originalLKneePos;

            boneLeftKnee.localRotation = Quaternion.identity; // 0° tekukan lutut (180° lurus vertikal)

        }



        if (boneRightKnee != null)

        {

            boneRightKnee.localPosition = originalRKneePos;

            boneRightKnee.localRotation = Quaternion.identity; // 0° tekukan lutut (180° lurus vertikal)

        }



        if (boneLeftFoot != null)

        {

            boneLeftFoot.localPosition = originalLFootPos;

            boneLeftFoot.localRotation = originalLFootRot;

        }



        if (boneRightFoot != null)

        {

            boneRightFoot.localPosition = originalRFootPos;

            boneRightFoot.localRotation = originalRFootRot;

        }



        if (boneLeftToe != null && UCharacterBindPoseMap.TryGetValue(boneLeftToe.name, out BoneBindData lToeBind))

        {

            boneLeftToe.localPosition = lToeBind.localPos;

            boneLeftToe.localRotation = lToeBind.localRot;

        }



        if (boneRightToe != null && UCharacterBindPoseMap.TryGetValue(boneRightToe.name, out BoneBindData rToeBind))

        {

            boneRightToe.localPosition = rToeBind.localPos;

            boneRightToe.localRotation = rToeBind.localRot;

        }

    }



    private Transform FindBoneExactOrKeyword(Transform root, string exactName, params string[] keywords)

    {

        if (root == null) return null;



        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)

        {

            Transform t = all[i];

            if (t == null || t == root) continue;

            if (t.name.StartsWith("VisJoint_") || t.name.StartsWith("Line_") || t.name == "VisLines_Holder") continue;

            if (t.name.Equals(exactName, StringComparison.OrdinalIgnoreCase))

            {

                return t;

            }

        }



        for (int i = 0; i < all.Length; i++)

        {

            Transform t = all[i];

            if (t == null || t == root) continue;

            if (t.name.StartsWith("VisJoint_") || t.name.StartsWith("Line_") || t.name == "VisLines_Holder") continue;

            string lower = t.name.ToLower();

            for (int k = 0; k < keywords.Length; k++)

            {

                if (lower.Contains(keywords[k]))

                {

                    return t;

                }

            }

        }



        return null;

    }



    /// <summary>

    /// Membaca metriks fisik avatar saat ini (ketinggian, sudut, dan sebaran sendi).

    /// </summary>

    public void UpdateLiveMetrics()

    {

        if (boneHips == null) CacheDummyBones();

        if (boneHips == null || referencePoseAvatar == null) return;



        // 1. Ketinggian Pinggul

        float groundY = referencePoseAvatar.transform.position.y;

        liveHipHeight = Mathf.Max(0f, boneHips.position.y - groundY);



        // 2. Ketinggian Lutut

        float leftKneeH = (boneLeftKnee != null) ? Mathf.Max(0f, boneLeftKnee.position.y - groundY) : liveHipHeight * 0.5f;

        float rightKneeH = (boneRightKnee != null) ? Mathf.Max(0f, boneRightKnee.position.y - groundY) : liveHipHeight * 0.5f;

        liveKneeHeight = Mathf.Min(leftKneeH, rightKneeH);



        // 3. Sudut Lekukan Lutut (180 = lurus vertikal, <90 = tekuk tajam)

        float leftAngle = (boneLeftThigh != null && boneLeftKnee != null && boneLeftFoot != null)

            ? Vector3.Angle(boneLeftKnee.position - boneLeftThigh.position, boneLeftFoot.position - boneLeftKnee.position)

            : 0f;

        float rightAngle = (boneRightThigh != null && boneRightKnee != null && boneRightFoot != null)

            ? Vector3.Angle(boneRightKnee.position - boneRightThigh.position, boneRightFoot.position - boneRightKnee.position)

            : 0f;

        leftAngle = 180f - leftAngle;

        rightAngle = 180f - rightAngle;

        liveKneeAngle = Mathf.Min(leftAngle, rightAngle);



        // 4. Sebaran Lutut (Knee Spread) - Pembeda Duduk Sila

        if (boneLeftKnee != null && boneRightKnee != null)

        {

            Vector2 lkXZ = new Vector2(boneLeftKnee.position.x, boneLeftKnee.position.z);

            Vector2 rkXZ = new Vector2(boneRightKnee.position.x, boneRightKnee.position.z);

            liveKneeSpread = Vector2.Distance(lkXZ, rkXZ);

        }

        else

        {

            liveKneeSpread = 0.35f;

        }



        // 5. Asimetri Ankle / Kaki - Pembeda Duduk Emok

        if (boneLeftFoot != null && boneRightFoot != null)

        {

            Vector3 hipRight = referencePoseAvatar.transform.right;

            float leftFootSide = Vector3.Dot(boneLeftFoot.position - boneHips.position, hipRight);

            float rightFootSide = Vector3.Dot(boneRightFoot.position - boneHips.position, hipRight);

            liveAnkleAsymmetry = Mathf.Abs((leftFootSide + rightFootSide) * 0.5f);

        }

        else

        {

            liveAnkleAsymmetry = 0f;

        }



        // 6. Kompresi Pinggul ke Kaki

        if (boneLeftFoot != null || boneRightFoot != null)

        {

            Vector3 fPos = (boneLeftFoot != null && boneRightFoot != null)

                ? (boneLeftFoot.position + boneRightFoot.position) * 0.5f

                : (boneLeftFoot != null ? boneLeftFoot.position : boneRightFoot.position);

            liveHipToFootDistance = Vector3.Distance(boneHips.position, fPos);

        }

    }



    /// <summary>

    /// Menghitung skor kecocokan persentase (%) untuk setiap label.

    /// Tidak akan memberi skor Berdiri 100% apabila lutut masih menekuk tajam (sudut lutut harus mendekati 180°).

    /// </summary>

    public void EvaluateAllScores()

    {

        allPostureScores.Clear();



        float refStandingHip = Mathf.Max(0.85f, standingHipHeight);



        // 1. Berdiri (Standing): Wajib pinggul tegak (~1.06m) DAN sudut lutut lurus mendekati 180° (>=172° untuk 100%)

        float hipDiffStanding = Mathf.Abs(liveHipHeight - refStandingHip);

        float hipScoreStanding = Mathf.Clamp01(1f - (hipDiffStanding / 0.22f));

        float kneeScoreStanding = (liveKneeAngle >= 172f)

            ? 1f

            : Mathf.Clamp01((liveKneeAngle - 125f) / (172f - 125f));

        float standingScore = hipScoreStanding * 0.50f + kneeScoreStanding * 0.50f;

        // Penalti tegas jika lutut masih menekuk (< 150°) atau pinggul turun jauh (< 0.75m)

        if (liveKneeAngle < 150f || liveHipHeight < 0.75f)

        {

            standingScore *= kneeScoreStanding * hipScoreStanding;

        }



        // 2. Berlutut (Kneeling)

        float kneelingScore = CalcGaussian(liveKneeHeight, kneelingKneeHeight, 0.08f) * 0.60f + CalcGaussian(liveHipHeight, kneelingHipHeight, 0.14f) * 0.40f;



        // 3. Jongkok (Squatting)

        float squatScore = CalcGaussian(liveHipHeight, squatHipHeight, 0.12f) * 0.45f + CalcGaussian(liveKneeAngle, squatKneeAngle, 20f) * 0.45f + (liveKneeHeight > 0.14f ? 0.10f : 0f);



        // 4. Duduk Sila (SittingCrossLegged)

        float silaScore = CalcGaussian(liveHipHeight, silaHipHeight, 0.10f) * 0.45f + CalcThreshold(liveKneeSpread, silaKneeSpread, 0.15f) * 0.55f;



        // 5. Duduk Emok (SittingEmok)

        float emokScore = CalcGaussian(liveHipHeight, emokHipHeight, 0.10f) * 0.40f + CalcThreshold(liveAnkleAsymmetry, emokAnkleAsymmetry, 0.08f) * 0.60f;



        // 6. Duduk Normal (SittingChair)

        float chairScore = CalcGaussian(liveHipHeight, chairHipHeight, 0.10f) * 0.60f + CalcGaussian(liveKneeAngle, chairKneeAngle, 25f) * 0.40f;



        // Evaluasi tambahan terhadap snapshot pose kustom yang sudah direkam user via tombol "Rekam"

        standingScore = Mathf.Max(standingScore, EvaluateCustomSnapshotScore(PostureType.Standing));

        kneelingScore = Mathf.Max(kneelingScore, EvaluateCustomSnapshotScore(PostureType.Kneeling));

        squatScore = Mathf.Max(squatScore, EvaluateCustomSnapshotScore(PostureType.Squatting));

        silaScore = Mathf.Max(silaScore, EvaluateCustomSnapshotScore(PostureType.SittingCrossLegged));

        emokScore = Mathf.Max(emokScore, EvaluateCustomSnapshotScore(PostureType.SittingEmok));

        chairScore = Mathf.Max(chairScore, EvaluateCustomSnapshotScore(PostureType.SittingChair));



        // Jika mode Showcase aktif, berikan 100% untuk pose yang sedang di-showcase (baik kustom maupun bawaan)

        if (isShowcaseMode)

        {

            bool hasCustomActive = HasRecordedPose(activeShowcasePose);

            switch (activeShowcasePose)

            {

                case PostureType.Standing:

                    if (hasCustomActive || (liveKneeAngle >= 170f && liveHipHeight >= 0.85f)) standingScore = 1f;

                    break;

                case PostureType.Kneeling:

                    if (hasCustomActive || (liveKneeHeight <= 0.20f && liveKneeAngle < 130f)) kneelingScore = 1f;

                    break;

                case PostureType.Squatting:

                    if (hasCustomActive || (liveKneeAngle < 110f && liveHipHeight < 0.70f)) squatScore = 1f;

                    break;

                case PostureType.SittingCrossLegged:

                    if (hasCustomActive || (liveKneeSpread >= 0.45f && liveHipHeight < 0.45f)) silaScore = 1f;

                    break;

                case PostureType.SittingEmok:

                    if (hasCustomActive || (liveAnkleAsymmetry >= 0.10f && liveHipHeight < 0.45f)) emokScore = 1f;

                    break;

                case PostureType.SittingChair:

                    if (hasCustomActive || (liveKneeAngle < 135f && liveHipHeight >= 0.50f)) chairScore = 1f;

                    break;

            }

        }



        // Simpan ke daftar

        AddScore(PostureType.Standing, "Berdiri", standingScore);

        AddScore(PostureType.Kneeling, "Berlutut", kneelingScore);

        AddScore(PostureType.Squatting, "Jongkok", squatScore);

        AddScore(PostureType.SittingCrossLegged, "Duduk Sila", silaScore);

        AddScore(PostureType.SittingEmok, "Duduk Emok", emokScore);

        AddScore(PostureType.SittingChair, "Duduk Normal", chairScore);



        // Cari pemenang tertinggi HANYA dari postur yang ENABLED (aktif)

        float maxPercent = -1f;

        int winnerIdx = -1;

        for (int i = 0; i < allPostureScores.Count; i++)

        {

            if (allPostureScores[i].isEnabled && allPostureScores[i].accuracyPercent > maxPercent)

            {

                maxPercent = allPostureScores[i].accuracyPercent;

                winnerIdx = i;

            }

        }



        for (int i = 0; i < allPostureScores.Count; i++)

        {

            allPostureScores[i].isDetectedWinner = (winnerIdx >= 0 && i == winnerIdx);

        }



        if (winnerIdx >= 0 && allPostureScores.Count > winnerIdx)

        {

            detectedPosture = allPostureScores[winnerIdx].postureType;

            detectedPostureLabel = allPostureScores[winnerIdx].labelIndonesian;

            topAccuracyPercent = allPostureScores[winnerIdx].accuracyPercent;

        }

        else

        {

            detectedPosture = PostureType.Standing;

            detectedPostureLabel = "Semua Nonaktif";

            topAccuracyPercent = 0f;

        }



        // Update status boolean realtime (hanya true jika postur tersebut enabled DAN pemenang)

        isBerdiri = (winnerIdx >= 0 && detectedPosture == PostureType.Standing);

        isJongkok = (winnerIdx >= 0 && detectedPosture == PostureType.Squatting);

        isBerlutut = (winnerIdx >= 0 && detectedPosture == PostureType.Kneeling);

        isDudukSila = (winnerIdx >= 0 && detectedPosture == PostureType.SittingCrossLegged);

        isDudukEmok = (winnerIdx >= 0 && detectedPosture == PostureType.SittingEmok);

        isDudukNormal = (winnerIdx >= 0 && detectedPosture == PostureType.SittingChair);

    }



    public Color GetPostureColor(PostureType posture)

    {

        if (tungkuTracker != null)

        {

            return tungkuTracker.GetPostureColor(posture);

        }



        switch (posture)

        {

            case PostureType.Squatting: return new Color(0f, 0.055f, 0.70f, 0.88f); // Biru Tua

            case PostureType.Kneeling: return new Color(0.0f, 0.68f, 0.94f, 0.88f); // Cyan / Biru Langit

            case PostureType.SittingCrossLegged: return new Color(0.55f, 0.18f, 0.79f, 0.88f); // Ungu / Violet

            case PostureType.SittingEmok: return new Color(0.90f, 0.22f, 0.53f, 0.88f); // Pink / Magenta

            case PostureType.SittingChair: return new Color(0.92f, 0.48f, 0.10f, 0.88f); // Oranye

            default: return new Color(0.72f, 0.08f, 0.08f, 0.88f); // Merah

        }

    }



    private void AddScore(PostureType type, string label, float raw01)

    {

        bool enabled = IsPostureEnabled(type);

        float percent = Mathf.Clamp(raw01 * 100f, 0f, 100f);

        allPostureScores.Add(new PostureScore(type, label, enabled, percent, false));

    }



    private float CalcGaussian(float value, float target, float tolerance)

    {

        float diff = value - target;

        return Mathf.Exp(-(diff * diff) / (2f * tolerance * tolerance));

    }



    private float CalcThreshold(float value, float threshold, float range)

    {

        if (value >= threshold) return 1f;

        float diff = threshold - value;

        if (diff >= range) return 0f;

        return 1f - (diff / range);

    }



    public bool HasRecordedPose(PostureType posture)

    {

        if (customRecordedPoses == null) return false;

        for (int i = 0; i < customRecordedPoses.Count; i++)

        {

            var snap = customRecordedPoses[i];

            if (snap != null && snap.postureType == posture && snap.hasCustomRecording && snap.boneTransforms != null && snap.boneTransforms.Count > 0)

            {

                return true;

            }

        }

        return false;

    }



    public RecordedPostureSnapshot GetRecordedPose(PostureType posture)

    {

        if (customRecordedPoses == null) return null;

        for (int i = 0; i < customRecordedPoses.Count; i++)

        {

            var snap = customRecordedPoses[i];

            if (snap != null && snap.postureType == posture && snap.hasCustomRecording && snap.boneTransforms != null && snap.boneTransforms.Count > 0)

            {

                return snap;

            }

        }

        return null;

    }



    public void ClearRecordedPose(PostureType posture)

    {

        if (customRecordedPoses != null)

        {

            for (int i = customRecordedPoses.Count - 1; i >= 0; i--)

            {

                if (customRecordedPoses[i] != null && customRecordedPoses[i].postureType == posture)

                {

                    customRecordedPoses.RemoveAt(i);

                }

            }

        }



        // Kembalikan ambang batas default untuk postur tersebut

        switch (posture)

        {

            case PostureType.Standing:

                standingHipHeight = 1.065582f;

                break;

            case PostureType.Kneeling:

                kneelingKneeHeight = 0.08f;

                kneelingHipHeight = 0.58f;

                break;

            case PostureType.Squatting:

                squatHipHeight = 0.48f;

                squatKneeAngle = 65f;

                break;

            case PostureType.SittingCrossLegged:

                silaHipHeight = 0.22f;

                silaKneeSpread = 0.60f;

                break;

            case PostureType.SittingEmok:

                emokHipHeight = 0.22f;

                emokAnkleAsymmetry = 0.16f;

                break;

            case PostureType.SittingChair:

                chairHipHeight = 0.70f;

                chairKneeAngle = 95f;

                break;

        }



        ShowcasePose(posture);

        Debug.Log($"[PoseCalibrationManager] Rekaman kustom untuk {posture} telah di-reset ke pose bawaan.");

    }



    private float EvaluateCustomSnapshotScore(PostureType posture)

    {

        RecordedPostureSnapshot snap = GetRecordedPose(posture);

        if (snap == null) return 0f;



        // Hitung kecocokan metriks fisik terhadap snapshot yang direkam

        float hipSim = CalcGaussian(liveHipHeight, snap.recordedHipHeight, 0.12f);

        float kneeAngleSim = CalcGaussian(liveKneeAngle, snap.recordedKneeAngle, 22f);

        float kneeHeightSim = CalcGaussian(liveKneeHeight, snap.recordedKneeHeight, 0.12f);

        float spreadSim = CalcGaussian(liveKneeSpread, snap.recordedKneeSpread, 0.15f);

        float asymSim = CalcGaussian(liveAnkleAsymmetry, snap.recordedAnkleAsymmetry, 0.10f);



        float metricScore = (hipSim * 0.30f) + (kneeAngleSim * 0.30f) + (kneeHeightSim * 0.15f) + (spreadSim * 0.15f) + (asymSim * 0.10f);



        // Hitung kecocokan rotasi tulang langsung pada dummy avatar jika ada

        if (referencePoseAvatar != null && snap.boneTransforms != null && snap.boneTransforms.Count > 0)

        {

            Transform[] allBones = referencePoseAvatar.GetComponentsInChildren<Transform>(true);

            float totalAngleDiff = 0f;

            int matchedCount = 0;



            for (int i = 0; i < snap.boneTransforms.Count; i++)

            {

                var saved = snap.boneTransforms[i];

                if (saved == null || string.IsNullOrEmpty(saved.boneName)) continue;



                for (int j = 0; j < allBones.Length; j++)

                {

                    Transform t = allBones[j];

                    if (t != null && t != referencePoseAvatar.transform && t.name == saved.boneName)

                    {

                        totalAngleDiff += Quaternion.Angle(t.localRotation, saved.localRotation);

                        matchedCount++;

                        break;

                    }

                }

            }



            if (matchedCount > 0)

            {

                float avgAngleDiff = totalAngleDiff / matchedCount;

                // Jika rata-rata selisih sudut seluruh sendi < 1.5 derajat, anggap 100% cocok dengan pose rekaman

                if (avgAngleDiff <= 1.5f && Mathf.Abs(liveHipHeight - snap.recordedHipHeight) <= 0.03f)

                {

                    return 1f;

                }

                float boneSim = Mathf.Clamp01(1f - (avgAngleDiff / 35f));

                return Mathf.Max(metricScore, (metricScore * 0.4f) + (boneSim * 0.6f));

            }

        }



        return metricScore;

    }



    // ==========================================

    // SHOWCASE POSE 100% (SIMETRIS & ANATOMIS / KUSTOM TEREKAM)

    // ==========================================

    /// <summary>

    /// Mengubah pose dummy avatar menjadi pose 100%.

    /// Jika user sudah pernah menekan tombol "Rekam" pada postur ini, maka seluruh rotasi & posisi sendi

    /// hasil rekaman user akan diterapkan kembali 100% persis. Jika belum, gunakan pose prosedural bawaan.

    /// </summary>

    public void ShowcasePose(PostureType posture)

    {

        if (referencePoseAvatar == null)

        {

            Debug.LogWarning("[PoseCalibrationManager] Silakan drag GameObject dummy avatar terlebih dahulu!");

            return;

        }



        // KUNCI POSISI WORLD: Catat posisi dan rotasi avatar saat ini agar TIDAK BISA teleport ke (0,0,0)

        Vector3 rootWorldPos = referencePoseAvatar.transform.position;

        Quaternion rootWorldRot = referencePoseAvatar.transform.rotation;

        Vector3 rootLocalScale = referencePoseAvatar.transform.localScale;



        SetPostureEnabled(posture, true);

        DisableDummyAvatarControllers();

        CacheDummyBones();

        if (boneHips == null) return;



        // Reset semua sendi ke T-Pose / Berdiri vertikal lurus murni terlebih dahulu

        RestoreOriginalPose();

        ApplyStraightStandingLegPose();



        // CEK APAKAH USER SUDAH MEREKAM POSE KUSTOM UNTUK POSTUR INI

        RecordedPostureSnapshot customSnap = GetRecordedPose(posture);

        if (customSnap != null && customSnap.hasCustomRecording && customSnap.boneTransforms != null && customSnap.boneTransforms.Count > 0)

        {

            Transform[] allBones = referencePoseAvatar.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < customSnap.boneTransforms.Count; i++)

            {

                SavedBoneTransform saved = customSnap.boneTransforms[i];

                if (saved == null || string.IsNullOrEmpty(saved.boneName)) continue;



                for (int j = 0; j < allBones.Length; j++)

                {

                    Transform t = allBones[j];

                    if (t == null || t == referencePoseAvatar.transform) continue;

                    if (t.name.StartsWith("VisJoint_") || t.name.StartsWith("Line_") || t.name == "VisLines_Holder") continue;



                    if (t.name == saved.boneName)

                    {

                        t.localPosition = saved.localPosition;

                        t.localRotation = saved.localRotation;

                        break;

                    }

                }

            }



            referencePoseAvatar.transform.position = rootWorldPos;

            referencePoseAvatar.transform.rotation = rootWorldRot;

            referencePoseAvatar.transform.localScale = rootLocalScale;



            isShowcaseMode = true;

            activeShowcasePose = posture;



            RefreshDummyVisualizer();

            UpdateLiveMetrics();

            EvaluateAllScores();

            Debug.Log($"<color=#00FF88>[PoseCalibrationManager] Showcase 100% (POSE REKAMAN USER) diterapkan untuk: {posture} (HipHeight={liveHipHeight:F2}m, KneeAngle={liveKneeAngle:F1}°)</color>");

            return;

        }



        Vector3 hipPos = originalHipsLocalPos;

        Vector3 charRight = referencePoseAvatar.transform.right;

        Vector3 charFwd = referencePoseAvatar.transform.forward;

        Vector3 charUp = referencePoseAvatar.transform.up;



        switch (posture)

        {

            case PostureType.Standing:

                // Berdiri tegak lurus sempurna: panggul penuh di atas (y = 1.065582), lutut 0° tekukan (180° lurus)

                boneHips.localPosition = hipPos;

                break;



            case PostureType.Kneeling:

                // Berlutut Simetris:

                hipPos.y -= 0.48f;

                boneHips.localPosition = hipPos;



                // Condongkan paha sedikit ke depan agar lutut menempel di tanah

                if (boneLeftThigh) boneLeftThigh.Rotate(charRight, -15f, Space.World);

                if (boneRightThigh) boneRightThigh.Rotate(charRight, -15f, Space.World);



                // Tekuk kedua lutut ke belakang 105 derajat simetris sempurna

                if (boneLeftKnee) boneLeftKnee.Rotate(charRight, 105f, Space.World);

                if (boneRightKnee) boneRightKnee.Rotate(charRight, 105f, Space.World);



                // Luruskan telapak kaki di lantai

                if (boneLeftFoot) boneLeftFoot.Rotate(charRight, -45f, Space.World);

                if (boneRightFoot) boneRightFoot.Rotate(charRight, -45f, Space.World);

                break;



            case PostureType.Squatting:

                // Jongkok Simetris (Pinggul turun ~0.48m dari lantai, lutut menekuk ~65°):

                hipPos.y -= 0.58f;

                boneHips.localPosition = hipPos;



                if (boneLeftThigh) boneLeftThigh.Rotate(charRight, -95f, Space.World);

                if (boneRightThigh) boneRightThigh.Rotate(charRight, -95f, Space.World);



                if (boneLeftKnee) boneLeftKnee.Rotate(charRight, 115f, Space.World);

                if (boneRightKnee) boneRightKnee.Rotate(charRight, 115f, Space.World);



                if (boneLeftFoot) boneLeftFoot.Rotate(charRight, -20f, Space.World);

                if (boneRightFoot) boneRightFoot.Rotate(charRight, -20f, Space.World);

                break;



            case PostureType.SittingCrossLegged:

                // Duduk Sila Simetris (Lutut terbuka lebar ke samping, telapak kaki menyilang ke tengah):

                hipPos.y -= 0.85f;

                boneHips.localPosition = hipPos;



                // 1. Tekuk paha dan lutut di bidang sagital terlebih dahulu

                if (boneLeftThigh) boneLeftThigh.Rotate(charRight, -95f, Space.World);

                if (boneRightThigh) boneRightThigh.Rotate(charRight, -95f, Space.World);



                if (boneLeftKnee) boneLeftKnee.Rotate(charRight, 125f, Space.World);

                if (boneRightKnee) boneRightKnee.Rotate(charRight, 125f, Space.World);



                // 2. Putar paha keluar agar betis melipat mendatar menyilang ke tengah

                if (boneLeftThigh)

                {

                    boneLeftThigh.Rotate(charFwd, 75f, Space.World);

                    boneLeftThigh.Rotate(charUp, -42f, Space.World);

                }

                if (boneRightThigh)

                {

                    boneRightThigh.Rotate(charFwd, -75f, Space.World);

                    boneRightThigh.Rotate(charUp, 42f, Space.World);

                }

                break;



            case PostureType.SittingEmok:

                // Duduk Emok (Bersimpuh menyamping):

                hipPos.y -= 0.84f;

                boneHips.localPosition = hipPos;



                // 1. Tekuk paha dan lutut di bidang sagital terlebih dahulu

                if (boneLeftThigh) boneLeftThigh.Rotate(charRight, -95f, Space.World);

                if (boneRightThigh) boneRightThigh.Rotate(charRight, -95f, Space.World);



                if (boneLeftKnee) boneLeftKnee.Rotate(charRight, 140f, Space.World);

                if (boneRightKnee) boneRightKnee.Rotate(charRight, 140f, Space.World);



                // 2. Rebahkan kedua betis searah ke samping sehingga asimetris di lantai

                if (boneLeftThigh)

                {

                    boneLeftThigh.Rotate(charFwd, 65f, Space.World);

                    boneLeftThigh.Rotate(charUp, -15f, Space.World);

                }

                if (boneRightThigh)

                {

                    boneRightThigh.Rotate(charFwd, 65f, Space.World);

                    boneRightThigh.Rotate(charUp, -15f, Space.World);

                }

                break;



            case PostureType.SittingChair:

                // Duduk Normal (Pinggul ~0.70m, paha mendatar, betis vertikal 95°):

                hipPos.y -= 0.36f;

                boneHips.localPosition = hipPos;



                if (boneLeftThigh) boneLeftThigh.Rotate(charRight, -85f, Space.World);

                if (boneRightThigh) boneRightThigh.Rotate(charRight, -85f, Space.World);



                if (boneLeftKnee) boneLeftKnee.Rotate(charRight, 85f, Space.World);

                if (boneRightKnee) boneRightKnee.Rotate(charRight, 85f, Space.World);

                break;

        }



        // PASTIKAN POSISI ROOT AVATAR DI SCENE TETAP TEPAT DI TEMPATNYA

        referencePoseAvatar.transform.position = rootWorldPos;

        referencePoseAvatar.transform.rotation = rootWorldRot;

        referencePoseAvatar.transform.localScale = rootLocalScale;



        isShowcaseMode = true;

        activeShowcasePose = posture;



        RefreshDummyVisualizer();

        UpdateLiveMetrics();

        EvaluateAllScores();

        Debug.Log($"[PoseCalibrationManager] Showcase 100% diaktifkan untuk pose: {posture} (HipHeight={liveHipHeight:F2}m, KneeAngle={liveKneeAngle:F1}°)");

    }



    /// <summary>

    /// Mengembalikan avatar dummy ke pose semula (T-Pose awal pabrik/FBX) tanpa mengubah posisi root di scene.

    /// </summary>

    public void RestoreOriginalPose()

    {

        if (referencePoseAvatar == null) return;



        // Kunci posisi root avatar agar tidak berpindah

        Vector3 rootWorldPos = referencePoseAvatar.transform.position;

        Quaternion rootWorldRot = referencePoseAvatar.transform.rotation;

        Vector3 rootLocalScale = referencePoseAvatar.transform.localScale;



        DisableDummyAvatarControllers();



        if (!hasOriginalPoseBackup || originalHipsLocalPos.y < 0.85f)

        {

            BackupOriginalPose();

        }



        // Kembalikan seluruh tulang anak ke bind pose murni FBX jika tersedia

        Transform[] allTransforms = referencePoseAvatar.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < allTransforms.Length; i++)

        {

            Transform t = allTransforms[i];

            if (t == null || t == referencePoseAvatar.transform) continue;

            if (t.name.StartsWith("VisJoint_") || t.name.StartsWith("Line_") || t.name == "VisLines_Holder") continue;



#if UNITY_EDITOR

            Transform source = PrefabUtility.GetCorrespondingObjectFromSource(t);

            if (source != null)

            {

                t.localPosition = source.localPosition;

                t.localRotation = source.localRotation;

                t.localScale = source.localScale;

                continue;

            }

#endif

            if (UCharacterBindPoseMap.TryGetValue(t.name, out BoneBindData bindData))

            {

                t.localPosition = bindData.localPos;

                t.localRotation = bindData.localRot;

                t.localScale = Vector3.one;

            }

        }



        if (boneHips != null)

        {

            boneHips.localPosition = originalHipsLocalPos;

            boneHips.localRotation = originalHipsLocalRot;

        }



        if (boneLeftThigh != null)

        {

            boneLeftThigh.localPosition = originalLThighPos;

            boneLeftThigh.localRotation = originalLThighRot;

        }

        if (boneRightThigh != null)

        {

            boneRightThigh.localPosition = originalRThighPos;

            boneRightThigh.localRotation = originalRThighRot;

        }

        if (boneLeftKnee != null)

        {

            boneLeftKnee.localPosition = originalLKneePos;

            boneLeftKnee.localRotation = originalLKneeRot;

        }

        if (boneRightKnee != null)

        {

            boneRightKnee.localPosition = originalRKneePos;

            boneRightKnee.localRotation = originalRKneeRot;

        }

        if (boneLeftFoot != null)

        {

            boneLeftFoot.localPosition = originalLFootPos;

            boneLeftFoot.localRotation = originalLFootRot;

        }

        if (boneRightFoot != null)

        {

            boneRightFoot.localPosition = originalRFootPos;

            boneRightFoot.localRotation = originalRFootRot;

        }



        referencePoseAvatar.transform.position = rootWorldPos;

        referencePoseAvatar.transform.rotation = rootWorldRot;

        referencePoseAvatar.transform.localScale = rootLocalScale;

    }



    // ==========================================

    // REKAM / CAPTURE SNAPSHOT DARI DUMMY SAAT INI

    // ==========================================

    public void CaptureCurrentPoseAs(PostureType posture)

    {

        SetPostureEnabled(posture, true);

        DisableDummyAvatarControllers();

        RefreshDummyVisualizer();

        UpdateLiveMetrics();



        // 1. Rekam seluruh posisi & rotasi lokal tulang dummy saat ini agar tombol Showcase menerapkan pose ini 100%

        if (referencePoseAvatar != null)

        {

            if (customRecordedPoses == null)

            {

                customRecordedPoses = new List<RecordedPostureSnapshot>();

            }



            RecordedPostureSnapshot snap = null;

            for (int i = 0; i < customRecordedPoses.Count; i++)

            {

                if (customRecordedPoses[i] != null && customRecordedPoses[i].postureType == posture)

                {

                    snap = customRecordedPoses[i];

                    break;

                }

            }



            if (snap == null)

            {

                snap = new RecordedPostureSnapshot { postureType = posture };

                customRecordedPoses.Add(snap);

            }



            snap.hasCustomRecording = true;

            snap.recordedHipHeight = liveHipHeight;

            snap.recordedKneeAngle = liveKneeAngle;

            snap.recordedKneeHeight = liveKneeHeight;

            snap.recordedKneeSpread = liveKneeSpread;

            snap.recordedAnkleAsymmetry = liveAnkleAsymmetry;



            if (snap.boneTransforms == null)

            {

                snap.boneTransforms = new List<SavedBoneTransform>();

            }

            else

            {

                snap.boneTransforms.Clear();

            }



            Transform[] allBones = referencePoseAvatar.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < allBones.Length; i++)

            {

                Transform t = allBones[i];

                if (t == null || t == referencePoseAvatar.transform) continue;

                if (t.name.StartsWith("VisJoint_") || t.name.StartsWith("Line_") || t.name == "VisLines_Holder") continue;



                snap.boneTransforms.Add(new SavedBoneTransform

                {

                    boneName = t.name,

                    localPosition = t.localPosition,

                    localRotation = t.localRotation

                });

            }

        }



        // 2. Simpan nilai metriks ke ambang batas acuan postur terkait

        switch (posture)

        {

            case PostureType.Squatting:

                squatHipHeight = liveHipHeight;

                squatKneeAngle = liveKneeAngle;

                Debug.Log($"<color=#00FF88>[PoseCalibrationManager] ✓ REKAM POSE JONGKOK BERHASIL: HipHeight={squatHipHeight:F2}m, KneeAngle={squatKneeAngle:F1}° (Seluruh rotasi sendi disimpan!)</color>");

                break;

            case PostureType.Kneeling:

                kneelingKneeHeight = liveKneeHeight;

                kneelingHipHeight = liveHipHeight;

                Debug.Log($"<color=#00FF88>[PoseCalibrationManager] ✓ REKAM POSE BERLUTUT BERHASIL: KneeHeight={kneelingKneeHeight:F2}m, HipHeight={kneelingHipHeight:F2}m (Seluruh rotasi sendi disimpan!)</color>");

                break;

            case PostureType.SittingCrossLegged:

                silaHipHeight = liveHipHeight;

                silaKneeSpread = liveKneeSpread;

                Debug.Log($"<color=#00FF88>[PoseCalibrationManager] ✓ REKAM POSE DUDUK SILA BERHASIL: HipHeight={silaHipHeight:F2}m, KneeSpread={silaKneeSpread:F2}m (Seluruh rotasi sendi disimpan!)</color>");

                break;

            case PostureType.SittingEmok:

                emokHipHeight = liveHipHeight;

                emokAnkleAsymmetry = liveAnkleAsymmetry;

                Debug.Log($"<color=#00FF88>[PoseCalibrationManager] ✓ REKAM POSE DUDUK EMOK BERHASIL: HipHeight={emokHipHeight:F2}m, AnkleAsymmetry={emokAnkleAsymmetry:F2}m (Seluruh rotasi sendi disimpan!)</color>");

                break;

            case PostureType.SittingChair:

                chairHipHeight = liveHipHeight;

                chairKneeAngle = liveKneeAngle;

                Debug.Log($"<color=#00FF88>[PoseCalibrationManager] ✓ REKAM POSE DUDUK NORMAL BERHASIL: HipHeight={chairHipHeight:F2}m, KneeAngle={chairKneeAngle:F1}° (Seluruh rotasi sendi disimpan!)</color>");

                break;

            case PostureType.Standing:

                standingHipHeight = Mathf.Max(0.50f, liveHipHeight);

                Debug.Log($"<color=#00FF88>[PoseCalibrationManager] ✓ REKAM POSE BERDIRI BERHASIL: HipHeight={liveHipHeight:F2}m, KneeAngle={liveKneeAngle:F1}° (Seluruh rotasi sendi disimpan!)</color>");

                break;

        }



        isShowcaseMode = true;

        activeShowcasePose = posture;

        EvaluateAllScores();

    }



    /// <summary>

    /// Menerapkan angka ambang batas hasil kalibrasi langsung ke PlayerTungkuTracker avatar asli.

    /// </summary>

    public void SyncFiltersToTracker()
    {
        if (tungkuTracker == null) FindReferences();
        if (tungkuTracker != null)
        {
            tungkuTracker.enableStanding = enableStanding;
            tungkuTracker.enableSquatting = enableSquatting;
            tungkuTracker.enableKneeling = enableKneeling;
            tungkuTracker.enableSittingCrossLegged = enableSittingCrossLegged;
            tungkuTracker.enableSittingEmok = enableSittingEmok;
            tungkuTracker.enableSittingChair = enableSittingChair;
        }
    }

    public void SyncThresholdsToTracker()

    {

        if (tungkuTracker == null)

        {

            tungkuTracker = FindObjectOfType<PlayerTungkuTracker>();

        }



        if (tungkuTracker != null)

        {

            tungkuTracker.enableSquatting = enableSquatting;

            tungkuTracker.enableKneeling = enableKneeling;

            tungkuTracker.enableSittingCrossLegged = enableSittingCrossLegged;

            tungkuTracker.enableSittingEmok = enableSittingEmok;

            tungkuTracker.enableSittingChair = enableSittingChair;



            tungkuTracker.squatHipsHeightThreshold = squatHipHeight + 0.07f;

            tungkuTracker.squatKneeAngleThreshold = squatKneeAngle + 10f;

            tungkuTracker.sitHipsHeightThreshold = chairHipHeight + 0.08f;

            tungkuTracker.sitKneeAngleThreshold = chairKneeAngle + 20f;

            tungkuTracker.kneelingKneeHeightThreshold = kneelingKneeHeight + 0.05f;

            tungkuTracker.kneelingHipHeightThreshold = kneelingHipHeight + 0.08f;

            tungkuTracker.sittingCrossLeggedKneeSpreadThreshold = silaKneeSpread - 0.08f;

            tungkuTracker.sittingEmokAnkleAsymmetryThreshold = emokAnkleAsymmetry - 0.04f;



            Debug.Log("<color=#00FF99><b>[PoseCalibrationManager] Berhasil menyinkronkan seluruh ambang batas kalibrasi ke PlayerTungkuTracker!</b></color>");

        }

        else

        {

            Debug.LogWarning("[PoseCalibrationManager] PlayerTungkuTracker tidak ditemukan di scene untuk disinkronkan.");

        }

    }



    // ==========================================

    // ON-SCREEN GUI (GAME VIEW)

    // ==========================================

    void OnGUI()

    {

        if (!showOnScreenGUI || !Application.isPlaying) return;



        int width = 340;

        int height = 380;

        int x = 20;

        int y = Screen.height - height - 30;



        GUI.Box(new Rect(x, y, width, height), $"<b>[POSE CALIBRATOR & SHOWCASE]</b>\n<size=9>Tekan {toggleGuiHotkey} untuk Hide</size>");



        GUILayout.BeginArea(new Rect(x + 10, y + 30, width - 20, height - 40));



        // Winner Banner

        GUI.backgroundColor = new Color(0.1f, 0.85f, 0.95f);

        GUILayout.Box($"<b>Terdeteksi: {detectedPostureLabel.ToUpper()} ({topAccuracyPercent:F1}%)</b>", GUILayout.Height(28));

        GUI.backgroundColor = Color.white;



        GUILayout.Space(4);

        GUILayout.Label("<b>Kesesuaian Label Lainnya (% Akurasi):</b>");



        for (int i = 0; i < allPostureScores.Count; i++)

        {

            var s = allPostureScores[i];

            if (!s.isEnabled)

            {

                GUILayout.Label($"<color=#666666>{s.labelIndonesian, -14}: [ (Nonaktif) ]</color>");

                continue;

            }

            string colorHex = s.isDetectedWinner ? "#00FF66" : (s.accuracyPercent > 50f ? "#FFCC00" : "#AAAAAA");

            string bar = new string('■', Mathf.RoundToInt(s.accuracyPercent / 8f)).PadRight(12, '·');

            GUILayout.Label($"<color={colorHex}>{s.labelIndonesian, -14}: [{bar}] <b>{s.accuracyPercent:F1}%</b></color>");

        }



        GUILayout.Space(6);

        GUILayout.Label("<b>Showcase Pose 100% (Dummy):</b>");



        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Berdiri", GUILayout.Height(22))) ShowcasePose(PostureType.Standing);

        if (GUILayout.Button("Berlutut", GUILayout.Height(22))) ShowcasePose(PostureType.Kneeling);

        if (GUILayout.Button("Jongkok", GUILayout.Height(22))) ShowcasePose(PostureType.Squatting);

        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Duduk Sila", GUILayout.Height(22))) ShowcasePose(PostureType.SittingCrossLegged);

        if (GUILayout.Button("Duduk Emok", GUILayout.Height(22))) ShowcasePose(PostureType.SittingEmok);

        if (GUILayout.Button("Duduk Normal", GUILayout.Height(22))) ShowcasePose(PostureType.SittingChair);

        GUILayout.EndHorizontal();



        GUILayout.Space(4);

        GUI.backgroundColor = new Color(0.2f, 0.9f, 0.5f);

        if (GUILayout.Button(">> Sync Hasil ke Player Tracker <<", GUILayout.Height(26)))

        {

            SyncThresholdsToTracker();

        }

        GUI.backgroundColor = Color.white;



        GUILayout.EndArea();

    }

}

