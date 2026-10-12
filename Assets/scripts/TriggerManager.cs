using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class KinectTriggerManager : MonoBehaviour
{
    public enum PostureRequirement
    {
        AnyState = 0,          // Bebas / Semua Postur
        SquattingOnly = 1,     // Khusus Jongkok
        SittingChairOnly = 2,  // Khusus Duduk Kursi
        SittingFloorOnly = 3,  // Khusus Duduk Selonjoran
        StandingOnly = 4,      // Khusus Berdiri
        SittingAny = 5         // Duduk Apa Saja (Kursi / Selonjoran)
    }

    [System.Serializable]
    public class TriggerSetting
    {
        [Header("Identitas Trigger & Barang")]
        [Tooltip("Nama zona collider / trigger ini (misal: 'Zona Tungku').")]
        public string zoneName = "Zona 1";

        [Tooltip("Nama barang atau objek interaksi di zona ini (misal: 'Tungku', 'Kayu Bakar', 'Wajan').")]
        public string namaBarang = "Barang";

        [Tooltip("Drag 3D GameObject atau Collider dari scene yang menjadi area pemicu trigger ini.")]
        public Collider zoneCollider;

        [Header("Pesan UI (Tampil di Panel Global)")]
        [TextArea(3, 6)]
        [Tooltip("Pesan unik yang akan ditampilkan di panel UI saat trigger ini aktif.")]
        public string inspectorMessage = "Kamu berada di area interaksi!";

        [Header("Audio SFX")]
        [Tooltip("Optional: Drop sound effect unik yang diputar saat zona ini terpicu.")]
        public AudioClip triggerSFX;

        [Tooltip("Apakah audio berputar terus (loop) selama user memenuhi syarat di dalam zona?")]
        public bool loopAudio = false;

        [Range(0.1f, 5.0f)]
        [Tooltip("Berapa detik waktu yang dibutuhkan audio untuk fade out halus saat keluar zona atau berganti postur?")]
        public float fadeOutDuration = 1.0f;

        [Header("Syarat Postur & Durasi")]
        [Tooltip("Syarat postur yang harus dipenuhi user untuk memicu zona ini.")]
        public PostureRequirement postureRequirement = PostureRequirement.AnyState;

        [Tooltip("Berapa detik berturut-turut user harus berada dalam postur ini di dalam zona sebelum event terpicu? Isi 0 untuk langsung aktif saat disentuh.")]
        public float continuousDurationRequired = 2.0f;

        [Tooltip("Jika true, sentuhan tangan langsung memicu trigger tanpa harus menunggu durasi continuousDurationRequired.")]
        public bool instantHandTouch = false;

        // Runtime states managed internally
        [HideInInspector] public bool isUserInside = false;
        [HideInInspector] public float conditionTimer = 0f;
        [HideInInspector] public bool eventHasFired = false;
        [HideInInspector] public AudioSource audioSource;
        [HideInInspector] public Coroutine fadeCoroutine;
        [HideInInspector] public HashSet<Collider> occupants = new HashSet<Collider>();
    }

    [Header("Global UI Message Overlay (Public)")]
    [Tooltip("Panel UI Pop-up/Overlay pesan notifikasi global. Cukup di-drag 1 kali ke sini (tidak perlu di setiap elemen trigger).")]
    public GameObject messagePanel;

    [Tooltip("TextMeshPro text component untuk JUDUL NAMA BARANG di dalam panel UI.")]
    public TMP_Text panelTitleField;

    [Tooltip("TextMeshPro text component untuk INFORMASI / PESAN di dalam panel UI tersebut.")]
    public TMP_Text panelTextField;

    [Header("Session Requirement")]
    [Tooltip("Jika true, trigger hanya aktif saat session logging KinectSessionManager sedang berjalan. Jika false, trigger selalu aktif untuk testing/in-game.")]
    public bool requireActiveSession = false;

    [Header("Global Trigger Configurations List")]
    [Tooltip("Daftar konfigurasi trigger zones. Klik '+' untuk menambah zona baru.")]
    public List<TriggerSetting> triggerZones = new List<TriggerSetting>();

    public static KinectTriggerManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (Instance == null) Instance = this;

        // Pastikan panel pesan global awalnya nonaktif
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        foreach (TriggerSetting zone in triggerZones)
        {
            if (zone.zoneCollider == null)
            {
                Debug.LogWarning($"[KinectTriggerManager] Entry '{zone.zoneName}' missing its 'Zone Collider' reference on: {gameObject.name}");
                continue;
            }

            zone.zoneCollider.isTrigger = true;

            // Pastikan zone collider memiliki kinematic Rigidbody agar PhysX menjamin trigger callbacks aktif
            Rigidbody zoneRb = zone.zoneCollider.GetComponent<Rigidbody>();
            if (zoneRb == null)
            {
                zoneRb = zone.zoneCollider.gameObject.AddComponent<Rigidbody>();
            }
            zoneRb.isKinematic = true;
            zoneRb.useGravity = false;

            // Inisialisasi audio channel per zona
            zone.audioSource = gameObject.AddComponent<AudioSource>();
            zone.audioSource.playOnAwake = false;
            zone.audioSource.spatialBlend = 0f; // 2D channel
            zone.audioSource.clip = zone.triggerSFX;
            zone.audioSource.loop = zone.loopAudio;

            TriggerZoneProxy proxy = zone.zoneCollider.gameObject.GetComponent<TriggerZoneProxy>();
            if (proxy == null) proxy = zone.zoneCollider.gameObject.AddComponent<TriggerZoneProxy>();

            proxy.Initialize(this, zone);
        }
    }

    void Update()
    {
        if (requireActiveSession && (KinectSessionManager.Instance == null || !KinectSessionManager.Instance.IsSessionActive))
            return;

        string activeKinectState = KinectSessionManager.Instance != null ? KinectSessionManager.Instance.CurrentState : "Standing";
        PostureType activePlayerPosture = PlayerTungkuTracker.Instance != null
            ? PlayerTungkuTracker.Instance.CurrentPostureType
            : PostureType.Standing;

        foreach (TriggerSetting zone in triggerZones)
        {
            if (!zone.isUserInside) continue;

            // Evaluasi syarat postur
            bool isConditionValid = CheckPostureCondition(zone.postureRequirement, activePlayerPosture, activeKinectState);

            // Skenario A: Event belum aktif, hitung durasi atau langsung tembak jika instantHandTouch / duration <= 0
            if (!zone.eventHasFired)
            {
                if (isConditionValid)
                {
                    if (zone.instantHandTouch || zone.continuousDurationRequired <= 0.05f)
                    {
                        FireZoneEvent(zone);
                    }
                    else
                    {
                        zone.conditionTimer += Time.deltaTime;

                        if (zone.conditionTimer >= zone.continuousDurationRequired)
                        {
                            FireZoneEvent(zone);
                        }
                    }
                }
                else
                {
                    zone.conditionTimer = 0f;
                }
            }
            // Skenario B: Event sedang aktif, namun user keluar dari syarat postur
            else
            {
                if (!isConditionValid)
                {
                    CancelActiveZoneEvent(zone);
                }
            }
        }
    }

    private bool CheckPostureCondition(PostureRequirement req, PostureType activePlayerPosture, string activeKinectState)
    {
        if (req == PostureRequirement.AnyState) return true;

        if (PlayerTungkuTracker.Instance != null)
        {
            switch (req)
            {
                case PostureRequirement.SquattingOnly:
                    return (activePlayerPosture == PostureType.Squatting);
                case PostureRequirement.SittingChairOnly:
                    return (activePlayerPosture == PostureType.SittingChair);
                case PostureRequirement.SittingFloorOnly:
                    return (activePlayerPosture == PostureType.SittingFloor);
                case PostureRequirement.StandingOnly:
                    return (activePlayerPosture == PostureType.Standing);
                case PostureRequirement.SittingAny:
                    return (activePlayerPosture == PostureType.SittingChair || activePlayerPosture == PostureType.SittingFloor);
            }
        }
        else
        {
            switch (req)
            {
                case PostureRequirement.SquattingOnly:
                    return (activeKinectState == "Squatting" || activeKinectState == "Jongkok");
                case PostureRequirement.SittingChairOnly:
                    return (activeKinectState == "Sitting" || activeKinectState == "SittingChair");
                case PostureRequirement.SittingFloorOnly:
                    return (activeKinectState == "SittingFloor");
                case PostureRequirement.StandingOnly:
                    return (activeKinectState == "Standing");
                case PostureRequirement.SittingAny:
                    return (activeKinectState == "Sitting" || activeKinectState == "SittingChair" || activeKinectState == "SittingFloor");
            }
        }

        return true;
    }

    private string GetTriggerLogName(TriggerSetting zone)
    {
        string name = !string.IsNullOrEmpty(zone.zoneName)
            ? zone.zoneName
            : (zone.zoneCollider != null ? zone.zoneCollider.gameObject.name : "TriggerZone");

        if (!string.IsNullOrEmpty(zone.namaBarang))
        {
            name += $" [{zone.namaBarang}]";
        }

        return name;
    }

    private void FireZoneEvent(TriggerSetting zone)
    {
        zone.eventHasFired = true;

        string loggedName = GetTriggerLogName(zone);

        // 1. Log ke session jika session sedang aktif
        if (KinectSessionManager.Instance != null && KinectSessionManager.Instance.IsSessionActive)
        {
            KinectSessionManager.Instance.TriggerEntered(loggedName);
        }

        // 2. Tampilkan panel UI popover global
        if (messagePanel != null)
        {
            messagePanel.SetActive(true);

            if (panelTitleField != null)
            {
                panelTitleField.text = !string.IsNullOrEmpty(zone.namaBarang) ? zone.namaBarang : zone.zoneName;
            }

            if (panelTextField != null)
            {
                panelTextField.text = zone.inspectorMessage;
            }
        }

        // 3. Mainkan Audio SFX
        if (zone.triggerSFX != null && zone.audioSource != null)
        {
            if (zone.fadeCoroutine != null) StopCoroutine(zone.fadeCoroutine);
            zone.audioSource.volume = 1f;
            zone.audioSource.Play();
        }

        Debug.Log($"<color=#00FFAA>[KinectTriggerManager]</color> <b>TRIGGER TERPICU:</b> {loggedName} | Pesan: '{zone.inspectorMessage}'");
    }

    private void CancelActiveZoneEvent(TriggerSetting zone)
    {
        zone.eventHasFired = false;
        zone.conditionTimer = 0f;

        // 1. Sembunyikan panel jika tidak ada trigger lain yang aktif
        if (messagePanel != null)
        {
            bool anyOtherActive = false;
            foreach (var otherZone in triggerZones)
            {
                if (otherZone != zone && otherZone.eventHasFired)
                {
                    anyOtherActive = true;
                    if (panelTitleField != null)
                    {
                        panelTitleField.text = !string.IsNullOrEmpty(otherZone.namaBarang) ? otherZone.namaBarang : otherZone.zoneName;
                    }
                    if (panelTextField != null)
                    {
                        panelTextField.text = otherZone.inspectorMessage;
                    }
                    break;
                }
            }

            if (!anyOtherActive)
            {
                messagePanel.SetActive(false);
            }
        }

        // 2. Kirim sinyal keluar ke session logger
        string loggedName = GetTriggerLogName(zone);
        if (KinectSessionManager.Instance != null && KinectSessionManager.Instance.IsSessionActive)
        {
            KinectSessionManager.Instance.TriggerExited(loggedName);
        }

        // 3. Fade Out audio
        if (zone.audioSource != null && zone.audioSource.isPlaying)
        {
            if (zone.fadeCoroutine != null) StopCoroutine(zone.fadeCoroutine);
            zone.fadeCoroutine = StartCoroutine(FadeOutAudioChannel(zone));
        }

        Debug.Log($"<color=#FF9900>[KinectTriggerManager]</color> <b>TRIGGER KELUAR/RESET:</b> {loggedName}");
    }

    private IEnumerator FadeOutAudioChannel(TriggerSetting zone)
    {
        AudioSource source = zone.audioSource;
        float startVolume = source.volume;
        float duration = zone.fadeOutDuration;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = 1f;
    }

    public void ProcessProxyEnter(TriggerSetting zone, Collider other)
    {
        if (zone == null) return;
        if (zone.occupants == null) zone.occupants = new HashSet<Collider>();

        if (IsValidUser(other))
        {
            if (other != null) zone.occupants.Add(other);
            zone.isUserInside = true;
            Debug.Log($"<color=#00FFAA>[KinectTriggerManager]</color> Collider masuk zona <b>{zone.zoneName}</b> ({zone.namaBarang}): '{(other != null ? other.name : "Unknown")}' (Total penghuni: {zone.occupants.Count})");
        }
    }

    public void ProcessProxyExit(TriggerSetting zone, Collider other)
    {
        if (zone == null) return;
        if (zone.occupants == null) zone.occupants = new HashSet<Collider>();

        if (other != null) zone.occupants.Remove(other);

        // Hanya batalkan jika semua collider pengguna telah meninggalkan zona
        if (zone.occupants.Count == 0)
        {
            zone.isUserInside = false;

            if (zone.eventHasFired)
            {
                CancelActiveZoneEvent(zone);
            }
            else
            {
                zone.conditionTimer = 0f;
            }
            Debug.Log($"<color=#FF9900>[KinectTriggerManager]</color> Pengguna telah meninggalkan zona <b>{zone.zoneName}</b> ({zone.namaBarang})");
        }
    }

    public bool IsValidUser(Collider other)
    {
        if (other == null) return false;

        // 1. Tag checks
        if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            return true;

        // 2. Name checks (Hand, Tangan, Joint, Avatar)
        string n = other.name.ToLowerInvariant();
        if (n.Contains("hand") || n.Contains("tangan") || n.Contains("joint") || n.Contains("avatar") || n.Contains("player"))
            return true;

        // 3. Script / Component hierarchy checks
        if (other.GetComponentInParent<PlayerHandColliderSetup>() != null ||
            other.GetComponentInParent<AvatarController>() != null ||
            other.GetComponent<HandTriggerReporter>() != null)
            return true;

        return false;
    }
}

// ==========================================
// LIGHTWEIGHT LINKING PROXY COMPONENT
// ==========================================
public class TriggerZoneProxy : MonoBehaviour
{
    private KinectTriggerManager manager;
    private KinectTriggerManager.TriggerSetting structuralZoneSettings;

    public void Initialize(KinectTriggerManager masterManager, KinectTriggerManager.TriggerSetting activeSettings)
    {
        manager = masterManager;
        structuralZoneSettings = activeSettings;
    }

    void OnTriggerEnter(Collider other)
    {
        if (manager != null && structuralZoneSettings != null)
        {
            manager.ProcessProxyEnter(structuralZoneSettings, other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (manager != null && structuralZoneSettings != null)
        {
            manager.ProcessProxyExit(structuralZoneSettings, other);
        }
    }

    /// <summary>
    /// Dipanggil langsung oleh PlayerHandColliderSetup (Overlap scan) agar tidak bergantung pada PhysX event semata.
    /// </summary>
    public void OnTriggerEnterExternal(Collider other, string sourceName)
    {
        if (manager != null && structuralZoneSettings != null)
        {
            manager.ProcessProxyEnter(structuralZoneSettings, other);
        }
    }

    /// <summary>
    /// Dipanggil langsung oleh PlayerHandColliderSetup saat tangan keluar dari zona.
    /// </summary>
    public void OnTriggerExitExternal(Collider other, string sourceName)
    {
        if (manager != null && structuralZoneSettings != null)
        {
            manager.ProcessProxyExit(structuralZoneSettings, other);
        }
    }
}
