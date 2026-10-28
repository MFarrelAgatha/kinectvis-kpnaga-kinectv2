using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class KinectTriggerManager : MonoBehaviour
{
    public enum PostureRequirement
    {
        AnyState = 0,               // Bebas / Semua Postur
        SquattingOnly = 1,          // Khusus Jongkok
        KneelingOnly = 2,           // Khusus Berlutut
        SittingCrossLeggedOnly = 3, // Khusus Duduk Sila
        SittingEmokOnly = 4,        // Khusus Duduk Emok
        SittingChairOnly = 5,       // Khusus Duduk Normal
        StandingOnly = 6,           // Khusus Berdiri
        SittingAny = 7              // Duduk Apa Saja (Sila / Emok / Normal)
    }

    public enum PanelPlacement
    {
        Front = 0,
        Bottom = 1,
        Left = 2,
        Right = 3,
        Top = 4,
        Back = 5,
        Center = 6
    }

    [System.Serializable]
    public class TriggerSetting : ISerializationCallbackReceiver
    {
        [Header("Identitas Trigger & Barang")]
        [Tooltip("Nama zona collider / trigger ini (misal: 'Zona Tungku').")]
        public string zoneName = "Zona 1";

        [Tooltip("Nama barang atau objek interaksi di zona ini (misal: 'Tungku', 'Kayu Bakar', 'Wajan').")]
        public string namaBarang = "Barang";

        [Tooltip("Daftar 3D GameObject atau Collider dari scene yang menjadi area pemicu trigger ini (bisa lebih dari satu).")]
        public List<Collider> zoneColliders = new List<Collider>();

        [UnityEngine.Serialization.FormerlySerializedAs("zoneCollider")]
        [SerializeField, HideInInspector]
        public Collider legacyZoneCollider;

        /// <summary>
        /// Property untuk backward-compatibility jika ada script lain yang membaca/menulis zoneCollider tunggal.
        /// </summary>
        public Collider zoneCollider
        {
            get
            {
                if (zoneColliders != null && zoneColliders.Count > 0)
                {
                    for (int i = 0; i < zoneColliders.Count; i++)
                    {
                        if (zoneColliders[i] != null) return zoneColliders[i];
                    }
                }
                return legacyZoneCollider;
            }
            set
            {
                if (zoneColliders == null) zoneColliders = new List<Collider>();
                if (zoneColliders.Count == 0)
                {
                    zoneColliders.Add(value);
                }
                else
                {
                    zoneColliders[0] = value;
                }
            }
        }

        [Header("Editor Scene Panel")]
        [Tooltip("Centang untuk menampilkan panel nama trigger ini di Scene View Editor. Uncheck untuk mematikan panel zona ini (disable each).")]
        public bool showPanel = true;

        [Tooltip("Dropdown letak panel terhadap GameObject / Collider (Front, Bottom, Left, Right, Top, Back, Center).")]
        public PanelPlacement panelPlacement = PanelPlacement.Front;

        [Tooltip("Jarak offset panel dari batas Collider / GameObject.")]
        public float panelOffset = 0.25f;

        [Tooltip("Offset koordinat manual tambahan (X, Y, Z).")]
        public Vector3 customOffset = Vector3.zero;

        [Tooltip("Opsional: Drag GameObject spesifik sebagai jangkar posisi panel. Jika kosong, otomatis memakai Collider zona.")]
        public GameObject panelTargetOverride;

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
        [HideInInspector] public List<TriggerZoneProxy> proxies = new List<TriggerZoneProxy>();

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (legacyZoneCollider != null)
            {
                if (zoneColliders == null) zoneColliders = new List<Collider>();
                if (!zoneColliders.Contains(legacyZoneCollider))
                {
                    zoneColliders.Add(legacyZoneCollider);
                }
            }
        }

        public int GetTotalOccupantCount()
        {
            if (proxies != null && proxies.Count > 0)
            {
                HashSet<Collider> uniqueOccupants = new HashSet<Collider>();
                for (int i = 0; i < proxies.Count; i++)
                {
                    if (proxies[i] == null || proxies[i].occupants == null) continue;
                    foreach (var col in proxies[i].occupants)
                    {
                        if (col != null && col.gameObject.activeInHierarchy)
                        {
                            uniqueOccupants.Add(col);
                        }
                    }
                }
                return uniqueOccupants.Count;
            }

            return occupants != null ? occupants.Count : 0;
        }
    }

    [Header("Global UI Message Overlay (Public)")]
    [Tooltip("Panel UI Pop-up/Overlay pesan notifikasi global. Cukup di-drag 1 kali ke sini (tidak perlu di setiap elemen trigger).")]
    public GameObject messagePanel;

    [Tooltip("TextMeshPro text component untuk JUDUL NAMA BARANG di dalam panel UI.")]
    public TMP_Text panelTitleField;

    [Tooltip("TextMeshPro text component untuk INFORMASI / PESAN di dalam panel UI tersebut.")]
    public TMP_Text panelTextField;

    [Header("Editor SceneView Panels (Editor Only)")]
    [Tooltip("Centang untuk mengaktifkan seluruh panel trigger di Scene View Editor. Uncheck untuk menonaktifkan SEMUA panel sekaligus (disable everything).")]
    public bool showEditorPanels = true;

    [Tooltip("Tampilkan garis wireframe kotak pembatas (bounds) zona collider di Scene View.")]
    public bool showWireframeBounds = true;

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
            // Migrasi legacy single collider jika ada
            if (zone.legacyZoneCollider != null)
            {
                if (zone.zoneColliders == null) zone.zoneColliders = new List<Collider>();
                if (!zone.zoneColliders.Contains(zone.legacyZoneCollider))
                {
                    zone.zoneColliders.Add(zone.legacyZoneCollider);
                }
            }

            // Inisialisasi audio channel per zona
            if (zone.audioSource == null)
            {
                zone.audioSource = gameObject.AddComponent<AudioSource>();
                zone.audioSource.playOnAwake = false;
                zone.audioSource.spatialBlend = 0f; // 2D channel
                zone.audioSource.clip = zone.triggerSFX;
                zone.audioSource.loop = zone.loopAudio;
            }

            if (zone.zoneColliders == null || zone.zoneColliders.Count == 0)
            {
                Debug.LogWarning($"[KinectTriggerManager] Entry '{zone.zoneName}' has no colliders assigned on: {gameObject.name}");
                continue;
            }

            if (zone.proxies == null) zone.proxies = new List<TriggerZoneProxy>();
            zone.proxies.Clear();

            int validColliderCount = 0;
            foreach (Collider col in zone.zoneColliders)
            {
                if (col == null) continue;

                validColliderCount++;
                col.isTrigger = true;

                // Pastikan zone collider memiliki kinematic Rigidbody agar PhysX menjamin trigger callbacks aktif
                Rigidbody zoneRb = col.GetComponent<Rigidbody>();
                if (zoneRb == null)
                {
                    zoneRb = col.gameObject.AddComponent<Rigidbody>();
                }
                zoneRb.isKinematic = true;
                zoneRb.useGravity = false;

                TriggerZoneProxy proxy = col.gameObject.GetComponent<TriggerZoneProxy>();
                if (proxy == null) proxy = col.gameObject.AddComponent<TriggerZoneProxy>();

                proxy.Initialize(this, zone, col);
                if (!zone.proxies.Contains(proxy))
                {
                    zone.proxies.Add(proxy);
                }
            }

            if (validColliderCount == 0)
            {
                Debug.LogWarning($"[KinectTriggerManager] Entry '{zone.zoneName}' missing all its Collider references on: {gameObject.name}");
            }
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
                case PostureRequirement.KneelingOnly:
                    return (activePlayerPosture == PostureType.Kneeling);
                case PostureRequirement.SittingCrossLeggedOnly:
                    return (activePlayerPosture == PostureType.SittingCrossLegged);
                case PostureRequirement.SittingEmokOnly:
                    return (activePlayerPosture == PostureType.SittingEmok);
                case PostureRequirement.SittingChairOnly:
                    return (activePlayerPosture == PostureType.SittingChair);
                case PostureRequirement.StandingOnly:
                    return (activePlayerPosture == PostureType.Standing);
                case PostureRequirement.SittingAny:
                    return (activePlayerPosture == PostureType.SittingChair || activePlayerPosture == PostureType.SittingCrossLegged || activePlayerPosture == PostureType.SittingEmok);
            }
        }
        else
        {
            switch (req)
            {
                case PostureRequirement.SquattingOnly:
                    return (activeKinectState == "Squatting" || activeKinectState == "Jongkok");
                case PostureRequirement.KneelingOnly:
                    return (activeKinectState == "Kneeling" || activeKinectState == "Berlutut");
                case PostureRequirement.SittingCrossLeggedOnly:
                    return (activeKinectState == "SittingCrossLegged" || activeKinectState == "Duduk Sila");
                case PostureRequirement.SittingEmokOnly:
                    return (activeKinectState == "SittingEmok" || activeKinectState == "Duduk Emok");
                case PostureRequirement.SittingChairOnly:
                    return (activeKinectState == "Sitting" || activeKinectState == "SittingChair" || activeKinectState == "Duduk Kursi" || activeKinectState == "Duduk Normal");
                case PostureRequirement.StandingOnly:
                    return (activeKinectState == "Standing" || activeKinectState == "Berdiri");
                case PostureRequirement.SittingAny:
                    return (activeKinectState == "Sitting" || activeKinectState == "SittingChair" || activeKinectState == "SittingCrossLegged" || activeKinectState == "SittingEmok" || activeKinectState == "Duduk Sila" || activeKinectState == "Duduk Emok" || activeKinectState == "Duduk Normal");
            }
        }

        return true;
    }

    private string GetTriggerLogName(TriggerSetting zone)
    {
        string defaultColName = "TriggerZone";
        if (zone.zoneColliders != null && zone.zoneColliders.Count > 0)
        {
            for (int i = 0; i < zone.zoneColliders.Count; i++)
            {
                if (zone.zoneColliders[i] != null)
                {
                    defaultColName = zone.zoneColliders[i].gameObject.name;
                    break;
                }
            }
        }
        else if (zone.legacyZoneCollider != null)
        {
            defaultColName = zone.legacyZoneCollider.gameObject.name;
        }

        string name = !string.IsNullOrEmpty(zone.zoneName)
            ? zone.zoneName
            : defaultColName;

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
        ProcessProxyEnter(zone, null, other);
    }

    public void ProcessProxyEnter(TriggerSetting zone, TriggerZoneProxy proxy, Collider other)
    {
        if (zone == null) return;
        if (zone.occupants == null) zone.occupants = new HashSet<Collider>();

        if (IsValidUser(other))
        {
            if (proxy != null)
            {
                if (proxy.occupants == null) proxy.occupants = new HashSet<Collider>();
                if (other != null) proxy.occupants.Add(other);
            }

            if (other != null) zone.occupants.Add(other);
            zone.isUserInside = true;

            int totalOccupants = zone.GetTotalOccupantCount();
            Debug.Log($"<color=#00FFAA>[KinectTriggerManager]</color> Collider masuk zona <b>{zone.zoneName}</b> ({zone.namaBarang}): '{(other != null ? other.name : "Unknown")}' (Total penghuni unik: {totalOccupants})");
        }
    }

    public void ProcessProxyExit(TriggerSetting zone, Collider other)
    {
        ProcessProxyExit(zone, null, other);
    }

    public void ProcessProxyExit(TriggerSetting zone, TriggerZoneProxy proxy, Collider other)
    {
        if (zone == null) return;
        if (zone.occupants == null) zone.occupants = new HashSet<Collider>();

        if (proxy != null && proxy.occupants != null && other != null)
        {
            proxy.occupants.Remove(other);
        }

        if (other != null && proxy == null)
        {
            zone.occupants.Remove(other);
        }

        int remainingOccupants = zone.GetTotalOccupantCount();

        // Hanya batalkan jika semua collider pengguna telah meninggalkan seluruh collider zona
        if (remainingOccupants == 0)
        {
            zone.occupants.Clear();
            zone.isUserInside = false;

            if (zone.eventHasFired)
            {
                CancelActiveZoneEvent(zone);
            }
            else
            {
                zone.conditionTimer = 0f;
            }
            Debug.Log($"<color=#FF9900>[KinectTriggerManager]</color> Pengguna telah meninggalkan seluruh collider zona <b>{zone.zoneName}</b> ({zone.namaBarang})");
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

#if UNITY_EDITOR
    private static Texture2D s_PanelBgTexNormal;
    private static Texture2D s_PanelBgTexActive;

    private static Texture2D GetOrCreateTex(ref Texture2D tex, Color color)
    {
        if (tex == null)
        {
            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
        }
        return tex;
    }

    void OnDrawGizmos()
    {
        if (!showEditorPanels) return;
        if (triggerZones == null || triggerZones.Count == 0) return;

        for (int i = 0; i < triggerZones.Count; i++)
        {
            TriggerSetting zone = triggerZones[i];
            if (zone == null || !zone.showPanel) continue;

            DrawZoneEditorPanel(zone, i);
        }
    }

    private void DrawZoneEditorPanel(TriggerSetting zone, int index)
    {
        // Aturan: Zona 0 adalah 1, selanjutnya bertambah terus (++1)
        int zoneNumber = index + 1;

        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;
        Transform primaryTransform = null;

        if (zone.panelTargetOverride != null)
        {
            primaryTransform = zone.panelTargetOverride.transform;
            Renderer rend = zone.panelTargetOverride.GetComponent<Renderer>();
            Collider col = zone.panelTargetOverride.GetComponent<Collider>();
            if (col != null)
            {
                combinedBounds = col.bounds;
                hasBounds = true;
            }
            else if (rend != null)
            {
                combinedBounds = rend.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds = new Bounds(primaryTransform.position, Vector3.one * 0.4f);
                hasBounds = true;
            }
        }
        else if (zone.zoneColliders != null && zone.zoneColliders.Count > 0)
        {
            for (int c = 0; c < zone.zoneColliders.Count; c++)
            {
                Collider col = zone.zoneColliders[c];
                if (col == null) continue;

                if (!hasBounds)
                {
                    combinedBounds = col.bounds;
                    primaryTransform = col.transform;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(col.bounds);
                }
            }
        }
        else if (zone.legacyZoneCollider != null)
        {
            combinedBounds = zone.legacyZoneCollider.bounds;
            primaryTransform = zone.legacyZoneCollider.transform;
            hasBounds = true;
        }

        if (!hasBounds)
        {
            primaryTransform = transform;
            combinedBounds = new Bounds(transform.position, Vector3.one * 0.4f);
        }

        Vector3 center = combinedBounds.center;
        Vector3 extents = combinedBounds.extents;
        Vector3 fwd = primaryTransform != null ? primaryTransform.forward : Vector3.forward;
        Vector3 rgt = primaryTransform != null ? primaryTransform.right : Vector3.right;

        Vector3 panelPos = center;
        float offset = zone.panelOffset;

        switch (zone.panelPlacement)
        {
            case PanelPlacement.Front:
                panelPos = center + fwd * (extents.z + offset);
                break;
            case PanelPlacement.Bottom:
                panelPos = center + Vector3.down * (extents.y + offset);
                break;
            case PanelPlacement.Left:
                panelPos = center - rgt * (extents.x + offset);
                break;
            case PanelPlacement.Right:
                panelPos = center + rgt * (extents.x + offset);
                break;
            case PanelPlacement.Top:
                panelPos = center + Vector3.up * (extents.y + offset);
                break;
            case PanelPlacement.Back:
                panelPos = center - fwd * (extents.z + offset);
                break;
            case PanelPlacement.Center:
            default:
                panelPos = center;
                break;
        }

        panelPos += zone.customOffset;

        // Visual: Garis wireframe bounds collider
        if (showWireframeBounds)
        {
            Gizmos.color = (Application.isPlaying && zone.isUserInside)
                ? new Color(0.2f, 1f, 0.4f, 0.6f)
                : new Color(0f, 0.85f, 1f, 0.35f);
            Gizmos.DrawWireCube(combinedBounds.center, combinedBounds.size);
        }

        // Visual: Garis & titik jangkar ke panel
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
        Gizmos.DrawLine(center, panelPos);
        Gizmos.DrawSphere(center, 0.03f);

        // GUI Style Panel Badge
        bool isActive = Application.isPlaying && zone.isUserInside;
        Texture2D bgTex = isActive
            ? GetOrCreateTex(ref s_PanelBgTexActive, new Color(0.1f, 0.45f, 0.25f, 0.92f))
            : GetOrCreateTex(ref s_PanelBgTexNormal, new Color(0.12f, 0.16f, 0.22f, 0.90f));

        GUIStyle style = new GUIStyle();
        style.normal.background = bgTex;
        style.normal.textColor = isActive ? new Color(1f, 0.95f, 0.3f) : Color.white;
        style.fontSize = 11;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.padding = new RectOffset(8, 8, 4, 4);

        string titleName = !string.IsNullOrEmpty(zone.zoneName) ? zone.zoneName : $"Zona {zoneNumber}";
        string barangStr = !string.IsNullOrEmpty(zone.namaBarang) ? $" [{zone.namaBarang}]" : "";
        string statusStr = isActive ? " ● ACTIVE" : "";

        string label = $" #{zoneNumber} : {titleName}{barangStr}{statusStr} ";

        UnityEditor.Handles.Label(panelPos, label, style);
    }
#endif
}

// ==========================================
// LIGHTWEIGHT LINKING PROXY COMPONENT
// ==========================================
public class TriggerZoneProxy : MonoBehaviour
{
    private KinectTriggerManager manager;
    private KinectTriggerManager.TriggerSetting structuralZoneSettings;
    public Collider associatedCollider;
    public HashSet<Collider> occupants = new HashSet<Collider>();

    public void Initialize(KinectTriggerManager masterManager, KinectTriggerManager.TriggerSetting activeSettings, Collider col = null)
    {
        manager = masterManager;
        structuralZoneSettings = activeSettings;
        associatedCollider = col != null ? col : GetComponent<Collider>();
        if (occupants == null) occupants = new HashSet<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (manager != null && structuralZoneSettings != null)
        {
            manager.ProcessProxyEnter(structuralZoneSettings, this, other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (manager != null && structuralZoneSettings != null)
        {
            manager.ProcessProxyExit(structuralZoneSettings, this, other);
        }
    }

    /// <summary>
    /// Dipanggil langsung oleh PlayerHandColliderSetup (Overlap scan) agar tidak bergantung pada PhysX event semata.
    /// </summary>
    public void OnTriggerEnterExternal(Collider other, string sourceName)
    {
        if (manager != null && structuralZoneSettings != null)
        {
            manager.ProcessProxyEnter(structuralZoneSettings, this, other);
        }
    }

    /// <summary>
    /// Dipanggil langsung oleh PlayerHandColliderSetup saat tangan keluar dari zona.
    /// </summary>
    public void OnTriggerExitExternal(Collider other, string sourceName)
    {
        if (manager != null && structuralZoneSettings != null)
        {
            manager.ProcessProxyExit(structuralZoneSettings, this, other);
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(KinectTriggerManager))]
public class KinectTriggerManagerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        KinectTriggerManager mgr = (KinectTriggerManager)target;

        UnityEditor.EditorGUILayout.Space(12);
        UnityEditor.EditorGUILayout.LabelField("Editor Scene Panels Controls", UnityEditor.EditorStyles.boldLabel);

        UnityEditor.EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.3f, 0.85f, 0.5f);
        if (GUILayout.Button("Enable All Panels", GUILayout.Height(28)))
        {
            mgr.showEditorPanels = true;
            if (mgr.triggerZones != null)
            {
                foreach (var z in mgr.triggerZones)
                {
                    if (z != null) z.showPanel = true;
                }
            }
            UnityEditor.EditorUtility.SetDirty(mgr);
            UnityEditor.SceneView.RepaintAll();
        }

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("Disable All Panels", GUILayout.Height(28)))
        {
            mgr.showEditorPanels = false;
            UnityEditor.EditorUtility.SetDirty(mgr);
            UnityEditor.SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;
        UnityEditor.EditorGUILayout.EndHorizontal();

        if (mgr.triggerZones != null && mgr.triggerZones.Count > 0)
        {
            UnityEditor.EditorGUILayout.Space(8);
            UnityEditor.EditorGUILayout.LabelField($"Trigger Zones Summary ({mgr.triggerZones.Count} Zones):", UnityEditor.EditorStyles.miniBoldLabel);
            for (int i = 0; i < mgr.triggerZones.Count; i++)
            {
                var z = mgr.triggerZones[i];
                if (z == null) continue;
                int num = i + 1; // 0 is 1 and goes ++
                string zTitle = !string.IsNullOrEmpty(z.zoneName) ? z.zoneName : $"Zona {num}";
                string bName = !string.IsNullOrEmpty(z.namaBarang) ? $"[{z.namaBarang}]" : "";
                string panelStatus = (mgr.showEditorPanels && z.showPanel) ? "ON" : "OFF";
                int colCount = z.zoneColliders != null ? z.zoneColliders.Count : (z.legacyZoneCollider != null ? 1 : 0);

                UnityEditor.EditorGUILayout.LabelField($"  #{num}: {zTitle} {bName} — Placement: {z.panelPlacement} | Panel: {panelStatus} | Colliders: {colCount}", UnityEditor.EditorStyles.miniLabel);
            }
        }
    }
}
#endif
