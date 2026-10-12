using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PlayerHandColliderSetup
/// Memasang trigger collider dan kinematic Rigidbody pada tangan kiri dan kanan avatar
/// untuk interaksi fisik yang presisi (misal: menyentuh Tungku atau trigger interaktif).
/// Memiliki scan overlap aktif (Physics.OverlapSphere) agar deteksi trigger 100% responsif
/// dan tidak terpengaruh limitasi PhysX static-trigger vs kinematic-trigger.
/// Menonaktifkan trigger perut (root capsule collider) agar trigger tidak terpicu oleh perut.
/// </summary>
[DisallowMultipleComponent]
public class PlayerHandColliderSetup : MonoBehaviour
{
    [Header("Pengaturan Collider Tangan")]
    [Tooltip("Radius bola collider pada telapak/pergelangan tangan (meter). Default: 0.15m")]
    [Range(0.05f, 0.40f)]
    public float handTriggerRadius = 0.15f;

    [Tooltip("Tag yang diberikan ke collider tangan agar cocok dengan TriggerLogger / sistem trigger. Default: 'Player'")]
    public string triggerTag = "Player";

    [Header("Deteksi Overlap Aktif (Scan Sphere)")]
    [Tooltip("Jika true, lakukan scan bola aktif tiap frame agar deteksi trigger tangan selalu bekerja meski PhysX collision matrix diabaikan.")]
    public bool activeScanOverlap = true;

    [Header("Belly / Perut Collider Safeguard")]
    [Tooltip("Jika true, nonaktifkan trigger pada CapsuleCollider di root avatar (perut) sehingga HANYA tangan yang memicu trigger.")]
    public bool disableBellyTrigger = true;

    [Header("Visual Debug (Scene View)")]
    [Tooltip("Gambar wireframe bola collider di Scene view pada tangan kiri dan kanan.")]
    public bool showGizmos = true;

    public Color gizmoLeftHandColor = new Color(0.2f, 0.9f, 1f, 0.85f);
    public Color gizmoRightHandColor = new Color(1f, 0.8f, 0.2f, 0.85f);

    [Header("Status Tangan Terdeteksi (Read-Only)")]
    [SerializeField] private Transform leftHandBone;
    [SerializeField] private Transform rightHandBone;
    [SerializeField] private SphereCollider leftHandCollider;
    [SerializeField] private SphereCollider rightHandCollider;
    [SerializeField] private CapsuleCollider rootBellyCollider;

    public Transform LeftHandBone => leftHandBone;
    public Transform RightHandBone => rightHandBone;

    private bool setupCompleted = false;
    private HashSet<Collider> leftHandOverlaps = new HashSet<Collider>();
    private HashSet<Collider> rightHandOverlaps = new HashSet<Collider>();

    void Awake()
    {
        SetupHandColliders();
    }

    void Start()
    {
        if (!setupCompleted)
        {
            SetupHandColliders();
        }
    }

    void Update()
    {
        if (!setupCompleted)
        {
            SetupHandColliders();
        }

        // Jalankan overlap scan per frame agar trigger detection 100% responsif & bebas glitch PhysX
        if (activeScanOverlap)
        {
            if (leftHandBone != null)
            {
                CheckHandOverlap(leftHandBone, "Tangan Kiri", ref leftHandOverlaps, leftHandCollider);
            }
            if (rightHandBone != null)
            {
                CheckHandOverlap(rightHandBone, "Tangan Kanan", ref rightHandOverlaps, rightHandCollider);
            }
        }
    }

    /// <summary>
    /// Mencari tulang tangan kiri & kanan dari AvatarController, Animator, atau hierarchy bone,
    /// lalu memasang SphereCollider dan kinematic Rigidbody.
    /// </summary>
    public void SetupHandColliders()
    {
        // 1. Cari via AvatarController jika terpasang pada avatar
        AvatarController ac = GetComponent<AvatarController>();
        if (ac == null) ac = GetComponentInParent<AvatarController>();
        if (ac == null) ac = GetComponentInChildren<AvatarController>();

        if (ac != null)
        {
            // Bone 7 = LeftHand, Bone 13 = RightHand pada standard AvatarController Kinect
            if (leftHandBone == null) leftHandBone = ac.GetBoneTransform(7);
            if (rightHandBone == null) rightHandBone = ac.GetBoneTransform(13);
        }

        // 2. Cari via Animator Mecanim
        if (leftHandBone == null || rightHandBone == null)
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                if (leftHandBone == null) leftHandBone = anim.GetBoneTransform(HumanBodyBones.LeftHand);
                if (leftHandBone == null) leftHandBone = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);

                if (rightHandBone == null) rightHandBone = anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHandBone == null) rightHandBone = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            }
        }

        // 3. Fallback pencarian nama bone (FBX model U_Character_REF: joint_HandLT, joint_HandRT)
        if (leftHandBone == null)
        {
            leftHandBone = FindBoneByName(transform, "joint_HandLT", "HandLT", "LeftHand", "Hand_L", "Hand.L", "PisiformLT", "Wrist_L", "WristLT");
        }
        if (rightHandBone == null)
        {
            rightHandBone = FindBoneByName(transform, "joint_HandRT", "HandRT", "RightHand", "Hand_R", "Hand.R", "PisiformRT", "Wrist_R", "WristRT");
        }

        // Pasang collider pada tangan kiri
        if (leftHandBone != null)
        {
            leftHandCollider = EnsureHandTriggerObject(leftHandBone, "HandTrigger_Left");
        }

        // Pasang collider pada tangan kanan
        if (rightHandBone != null)
        {
            rightHandCollider = EnsureHandTriggerObject(rightHandBone, "HandTrigger_Right");
        }

        // Atur Belly Collider (CapsuleCollider perut di root)
        ConfigureBellyCollider();

        if (leftHandBone != null && rightHandBone != null)
        {
            setupCompleted = true;
            Debug.Log($"<color=#00FFAA>[PlayerHandColliderSetup]</color> Hand colliders aktif pada bone: L='{leftHandBone.name}', R='{rightHandBone.name}' (radius {handTriggerRadius}m)");
        }
    }

    private SphereCollider EnsureHandTriggerObject(Transform bone, string childName)
    {
        Transform child = bone.Find(childName);
        GameObject triggerObj;

        if (child == null)
        {
            triggerObj = new GameObject(childName);
            triggerObj.transform.SetParent(bone, false);
            triggerObj.transform.localPosition = Vector3.zero;
            triggerObj.transform.localRotation = Quaternion.identity;
        }
        else
        {
            triggerObj = child.gameObject;
        }

        // Berikan tag Player
        if (!string.IsNullOrEmpty(triggerTag))
        {
            try
            {
                triggerObj.tag = triggerTag;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlayerHandColliderSetup] Tidak dapat memberi tag '{triggerTag}': {ex.Message}");
            }
        }

        // Pastikan ada Kinematic Rigidbody
        Rigidbody rb = triggerObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = triggerObj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        // Pasang SphereCollider
        SphereCollider sc = triggerObj.GetComponent<SphereCollider>();
        if (sc == null)
        {
            sc = triggerObj.AddComponent<SphereCollider>();
        }
        sc.isTrigger = true;
        sc.radius = handTriggerRadius;

        // Pasang reporter script
        HandTriggerReporter reporter = triggerObj.GetComponent<HandTriggerReporter>();
        if (reporter == null)
        {
            reporter = triggerObj.AddComponent<HandTriggerReporter>();
        }
        reporter.handName = childName.Contains("Left") ? "Tangan Kiri" : "Tangan Kanan";

        return sc;
    }

    private void CheckHandOverlap(Transform bone, string handName, ref HashSet<Collider> currentOverlaps, SphereCollider handCollider)
    {
        if (bone == null) return;

        Collider[] hits = Physics.OverlapSphere(bone.position, handTriggerRadius, ~0, QueryTriggerInteraction.Collide);
        HashSet<Collider> newOverlaps = new HashSet<Collider>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;

            // Jangan deteksi diri sendiri (collider tubuh sendiri)
            if (col.transform.IsChildOf(transform) || col.transform == transform) continue;

            newOverlaps.Add(col);
        }

        Collider reportingCol = handCollider != null ? handCollider : GetComponent<Collider>();

        // Objek yang baru saja disentuh tangan (Enter)
        foreach (Collider col in newOverlaps)
        {
            if (!currentOverlaps.Contains(col))
            {
                // 1. Teruskan ke TriggerZoneProxy (KinectTriggerManager)
                TriggerZoneProxy proxy = col.GetComponent<TriggerZoneProxy>();
                if (proxy == null) proxy = col.GetComponentInParent<TriggerZoneProxy>();
                if (proxy != null)
                {
                    proxy.OnTriggerEnterExternal(reportingCol, handName);
                }

                // 2. Teruskan ke TriggerLogger jika ada
                TriggerLogger tl = col.GetComponent<TriggerLogger>();
                if (tl == null) tl = col.GetComponentInParent<TriggerLogger>();
                if (tl != null && KinectSessionManager.Instance != null && KinectSessionManager.Instance.IsSessionActive)
                {
                    KinectSessionManager.Instance.TriggerEntered(tl.triggerName);
                }

                // 3. Deteksi Tungku langsung
                if (col.gameObject.name.IndexOf("Tungku", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    col.transform.root.name.IndexOf("Tungku", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (KinectSessionManager.Instance != null && KinectSessionManager.Instance.IsSessionActive)
                    {
                        KinectSessionManager.Instance.TriggerEntered("Tungku");
                    }
                }
            }
        }

        // Objek yang ditinggalkan tangan (Exit)
        foreach (Collider col in currentOverlaps)
        {
            if (!newOverlaps.Contains(col))
            {
                TriggerZoneProxy proxy = col.GetComponent<TriggerZoneProxy>();
                if (proxy == null) proxy = col.GetComponentInParent<TriggerZoneProxy>();
                if (proxy != null)
                {
                    proxy.OnTriggerExitExternal(reportingCol, handName);
                }

                TriggerLogger tl = col.GetComponent<TriggerLogger>();
                if (tl == null) tl = col.GetComponentInParent<TriggerLogger>();
                if (tl != null && KinectSessionManager.Instance != null && KinectSessionManager.Instance.IsSessionActive)
                {
                    KinectSessionManager.Instance.TriggerExited(tl.triggerName);
                }
            }
        }

        currentOverlaps = newOverlaps;
    }

    private void ConfigureBellyCollider()
    {
        if (rootBellyCollider == null)
        {
            rootBellyCollider = GetComponent<CapsuleCollider>();
        }

        if (rootBellyCollider != null)
        {
            if (disableBellyTrigger)
            {
                // Nonaktifkan isTrigger pada root CapsuleCollider agar perut TIDAK memicu trigger!
                rootBellyCollider.isTrigger = false;
            }
            else
            {
                rootBellyCollider.isTrigger = true;
            }
        }
    }

    void OnValidate()
    {
        if (leftHandCollider != null) leftHandCollider.radius = handTriggerRadius;
        if (rightHandCollider != null) rightHandCollider.radius = handTriggerRadius;
        ConfigureBellyCollider();
    }

    private Transform FindBoneByName(Transform parent, params string[] candidateNames)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            foreach (string name in candidateNames)
            {
                if (child.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child;
                }
            }
        }
        return null;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (leftHandBone != null)
        {
            Gizmos.color = gizmoLeftHandColor;
            Gizmos.DrawWireSphere(leftHandBone.position, handTriggerRadius);
        }

        if (rightHandBone != null)
        {
            Gizmos.color = gizmoRightHandColor;
            Gizmos.DrawWireSphere(rightHandBone.position, handTriggerRadius);
        }
    }
}

/// <summary>
/// Helper untuk mendeteksi dan meneruskan event sentuhan tangan ke KinectTriggerManager / KinectSessionManager / TriggerLogger.
/// </summary>
public class HandTriggerReporter : MonoBehaviour
{
    public string handName = "Tangan";

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // 1. Cek TriggerZoneProxy (KinectTriggerManager)
        TriggerZoneProxy proxy = other.GetComponent<TriggerZoneProxy>();
        if (proxy == null) proxy = other.GetComponentInParent<TriggerZoneProxy>();
        if (proxy != null)
        {
            proxy.OnTriggerEnterExternal(GetComponent<Collider>(), handName);
            Debug.Log($"<color=#00FFAA>[HandTrigger]</color> <b>{handName}</b> menyentuh zona proxy: <b>{other.gameObject.name}</b>");
            return;
        }

        // 2. Jika menyentuh trigger yang memiliki TriggerLogger
        TriggerLogger tl = other.GetComponent<TriggerLogger>();
        if (tl == null) tl = other.GetComponentInParent<TriggerLogger>();
        if (tl != null)
        {
            if (KinectSessionManager.Instance != null && KinectSessionManager.Instance.IsSessionActive)
            {
                KinectSessionManager.Instance.TriggerEntered(tl.triggerName);
            }
            Debug.Log($"<color=#00FFAA>[HandTrigger]</color> <b>{handName}</b> menyentuh trigger: <b>{tl.triggerName}</b> ({other.gameObject.name})");
            return;
        }

        // 3. Cek jika objek yang disentuh bernama Tungku
        if (other.gameObject.name.IndexOf("Tungku", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (KinectSessionManager.Instance != null && KinectSessionManager.Instance.IsSessionActive)
            {
                KinectSessionManager.Instance.TriggerEntered("Tungku");
            }
            Debug.Log($"<color=#00FFAA>[HandTrigger]</color> <b>{handName}</b> menyentuh Tungku!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        TriggerZoneProxy proxy = other.GetComponent<TriggerZoneProxy>();
        if (proxy == null) proxy = other.GetComponentInParent<TriggerZoneProxy>();
        if (proxy != null)
        {
            proxy.OnTriggerExitExternal(GetComponent<Collider>(), handName);
            Debug.Log($"<color=#FF9900>[HandTrigger]</color> <b>{handName}</b> keluar dari zona proxy: <b>{other.gameObject.name}</b>");
            return;
        }

        TriggerLogger tl = other.GetComponent<TriggerLogger>();
        if (tl == null) tl = other.GetComponentInParent<TriggerLogger>();
        if (tl != null)
        {
            if (KinectSessionManager.Instance != null && KinectSessionManager.Instance.IsSessionActive)
            {
                KinectSessionManager.Instance.TriggerExited(tl.triggerName);
            }
            Debug.Log($"<color=#FF9900>[HandTrigger]</color> <b>{handName}</b> keluar dari trigger: <b>{tl.triggerName}</b>");
        }
    }
}
