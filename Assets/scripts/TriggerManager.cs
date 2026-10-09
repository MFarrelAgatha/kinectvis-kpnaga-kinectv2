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

        [Tooltip("Berapa detik berturut-turut user harus berada dalam postur ini di dalam zona sebelum event terpicu?")]
        public float continuousDurationRequired = 2.0f;

        // Runtime states managed internally
        [HideInInspector] public bool isUserInside = false;
        [HideInInspector] public float conditionTimer = 0f;
        [HideInInspector] public bool eventHasFired = false;
        [HideInInspector] public AudioSource audioSource;
        [HideInInspector] public Coroutine fadeCoroutine;
    }

    [Header("Global UI Message Overlay (Public)")]
    [Tooltip("Panel UI Pop-up/Overlay pesan notifikasi global. Cukup di-drag 1 kali ke sini (tidak perlu di setiap elemen trigger).")]
    public GameObject messagePanel;

    [Tooltip("TextMeshPro text component untuk JUDUL NAMA BARANG di dalam panel UI.")]
    public TMP_Text panelTitleField;

    [Tooltip("TextMeshPro text component untuk INFORMASI / PESAN di dalam panel UI tersebut.")]
    public TMP_Text panelTextField;

    [Header("Global Trigger Configurations List")]
    [Tooltip("Daftar konfigurasi trigger zones. Klik '+' untuk menambah zona baru.")]
    public List<TriggerSetting> triggerZones = new List<TriggerSetting>();

    void Start()
    {
        // Pastikan panel pesan global awalnya nonaktif
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        foreach (TriggerSetting zone in triggerZones)
        {
            if (zone.zoneCollider == null)
            {
                Debug.LogWarning($"[TriggerManager] Entry '{zone.zoneName}' missing its 'Zone Collider' reference on: {gameObject.name}");
                continue;
            }

            zone.zoneCollider.isTrigger = true;

            // DYNAMIC HYBRID AUDIO: Creates an isolated 2D channel right on this manager per zone item
            zone.audioSource = gameObject.AddComponent<AudioSource>();
            zone.audioSource.playOnAwake = false;
            zone.audioSource.spatialBlend = 0f; // Hard-coded to 2D for loud, clear room-wide sound
            zone.audioSource.clip = zone.triggerSFX;
            zone.audioSource.loop = zone.loopAudio;

            TriggerZoneProxy proxy = zone.zoneCollider.gameObject.GetComponent<TriggerZoneProxy>();
            if (proxy == null) proxy = zone.zoneCollider.gameObject.AddComponent<TriggerZoneProxy>();

            proxy.Initialize(this, zone);
        }
    }

    void Update()
    {
        if (KinectSessionManager.Instance == null || !KinectSessionManager.Instance.IsSessionActive)
            return;

        string activeKinectState = KinectSessionManager.Instance.CurrentState;
        PostureType activePlayerPosture = PlayerTungkuTracker.Instance != null
            ? PlayerTungkuTracker.Instance.CurrentPostureType
            : PostureType.Standing;

        foreach (TriggerSetting zone in triggerZones)
        {
            if (!zone.isUserInside) continue;

            // Evaluate if user is currently matching the posture rule criteria
            bool isConditionValid = false;

            if (PlayerTungkuTracker.Instance != null)
            {
                switch (zone.postureRequirement)
                {
                    case PostureRequirement.AnyState:
                        isConditionValid = true;
                        break;
                    case PostureRequirement.SquattingOnly:
                        isConditionValid = (activePlayerPosture == PostureType.Squatting);
                        break;
                    case PostureRequirement.SittingChairOnly:
                        isConditionValid = (activePlayerPosture == PostureType.SittingChair);
                        break;
                    case PostureRequirement.SittingFloorOnly:
                        isConditionValid = (activePlayerPosture == PostureType.SittingFloor);
                        break;
                    case PostureRequirement.StandingOnly:
                        isConditionValid = (activePlayerPosture == PostureType.Standing);
                        break;
                    case PostureRequirement.SittingAny:
                        isConditionValid = (activePlayerPosture == PostureType.SittingChair || activePlayerPosture == PostureType.SittingFloor);
                        break;
                }
            }
            else
            {
                switch (zone.postureRequirement)
                {
                    case PostureRequirement.AnyState:
                        isConditionValid = (activeKinectState == "Sitting" || activeKinectState == "Standing" || activeKinectState == "Squatting");
                        break;
                    case PostureRequirement.SquattingOnly:
                        isConditionValid = (activeKinectState == "Squatting" || activeKinectState == "Jongkok");
                        break;
                    case PostureRequirement.SittingChairOnly:
                        isConditionValid = (activeKinectState == "Sitting" || activeKinectState == "SittingChair");
                        break;
                    case PostureRequirement.SittingFloorOnly:
                        isConditionValid = (activeKinectState == "SittingFloor");
                        break;
                    case PostureRequirement.StandingOnly:
                        isConditionValid = (activeKinectState == "Standing");
                        break;
                    case PostureRequirement.SittingAny:
                        isConditionValid = (activeKinectState == "Sitting" || activeKinectState == "SittingChair" || activeKinectState == "SittingFloor");
                        break;
                }
            }

            // SCENARIO A: The event hasn't happened yet. Keep counting up.
            if (!zone.eventHasFired)
            {
                if (isConditionValid)
                {
                    zone.conditionTimer += Time.deltaTime;

                    if (zone.conditionTimer >= zone.continuousDurationRequired)
                    {
                        FireZoneEvent(zone);
                    }
                }
                else
                {
                    zone.conditionTimer = 0f;
                }
            }
            // SCENARIO B: Event is active, but user broke their tracking pose ("doing outside the term")
            else
            {
                if (!isConditionValid)
                {
                    CancelActiveZoneEvent(zone);
                }
            }
        }
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

        // 1. Alert main logger pipeline to append lines to CSV file
        if (KinectSessionManager.Instance != null)
        {
            KinectSessionManager.Instance.TriggerEntered(loggedName);
        }

        // 2. Display the UI popover message overlay (Global Panel)
        if (messagePanel != null)
        {
            messagePanel.SetActive(true);

            // Set Judul Barang
            if (panelTitleField != null)
            {
                panelTitleField.text = !string.IsNullOrEmpty(zone.namaBarang) ? zone.namaBarang : zone.zoneName;
            }

            // Set Info / Pesan
            if (panelTextField != null)
            {
                panelTextField.text = zone.inspectorMessage;
            }
        }

        // 3. Audio engine playback initialization
        if (zone.triggerSFX != null && zone.audioSource != null)
        {
            // Halt any lingering fade-out sequences and reset volume to full capacity
            if (zone.fadeCoroutine != null) StopCoroutine(zone.fadeCoroutine);
            zone.audioSource.volume = 1f;
            zone.audioSource.Play();
        }
    }

    private void CancelActiveZoneEvent(TriggerSetting zone)
    {
        zone.eventHasFired = false;
        zone.conditionTimer = 0f;

        // 1. Instantly hide UI panels when terms break (or show other still-active zone)
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

        // 2. Safely communicate exit to master log loops
        string loggedName = GetTriggerLogName(zone);
        if (KinectSessionManager.Instance != null)
        {
            KinectSessionManager.Instance.TriggerExited(loggedName);
        }

        // 3. Initialize the asynchronous Audio Fade Out
        if (zone.audioSource != null && zone.audioSource.isPlaying)
        {
            if (zone.fadeCoroutine != null) StopCoroutine(zone.fadeCoroutine);
            zone.fadeCoroutine = StartCoroutine(FadeOutAudioChannel(zone));
        }
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
            // Linearly interpolate current volume towards absolute zero over time
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = 1f; // Reset default structural volume back to max for its next trigger lifecycle
    }

    public void ProcessProxyEnter(TriggerSetting zone, Collider other)
    {
        if (IsValidUser(other))
        {
            zone.isUserInside = true;
            zone.conditionTimer = 0f;
        }
    }

    public void ProcessProxyExit(TriggerSetting zone, Collider other)
    {
        if (IsValidUser(other))
        {
            zone.isUserInside = false;

            // If they completely walk outside the bounds while the event is running, drop it and fade audio
            if (zone.eventHasFired)
            {
                CancelActiveZoneEvent(zone);
            }
            else
            {
                zone.conditionTimer = 0f;
            }
        }
    }

    private bool IsValidUser(Collider other)
    {
        return other.CompareTag("Player") || other.name.Contains("Joint") || other.name.Contains("Avatar") || (other.transform.root != null && other.transform.root.CompareTag("Player"));
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
}
