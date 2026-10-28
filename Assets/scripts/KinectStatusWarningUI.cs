using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Windows.Kinect;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// KinectStatusWarningUI
/// Menampilkan icon indikator warning di pojok kanan atas layar ketika sensor Xbox Kinect belum terdeteksi
/// atau dicabut kabelnya secara fisik saat aplikasi berjalan.
/// Icon menyala/berdenyut saat terputus, dan saat di-hover dengan kursor mouse akan memunculkan tooltip pop-up informasi.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class KinectStatusWarningUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("1. UI Elements")]
    [Tooltip("Canvas target tempat meletakkan icon warning. Jika kosong, otomatis mencari Canvas aktif di scene.")]
    public Canvas targetCanvas;

    [Tooltip("Komponen CanvasGroup pada icon untuk mengontrol visibilitas tanpa mematikan script Update().")]
    public CanvasGroup iconCanvasGroup;

    [Tooltip("Komponen Image untuk icon warning.")]
    public Image warningIconImage;

    [Tooltip("Panel pop-up tooltip yang muncul saat icon di-hover.")]
    public GameObject tooltipPanel;

    [Tooltip("Komponen TextMeshPro untuk judul tooltip.")]
    public TMP_Text tooltipTitleText;

    [Tooltip("Komponen TextMeshPro untuk teks deskripsi detail pada tooltip.")]
    public TMP_Text tooltipBodyText;

    [Header("2. Tampilan & Posisi")]
    [Tooltip("Sprite gambar icon warning (Default: Assets/2D/warning_kinect.png).")]
    public Sprite warningSprite;

    [Tooltip("Posisi koordinat dari pojok kanan atas (Anchor Top-Right: 1, 1).")]
    public Vector2 anchoredPosition = new Vector2(-42f, -42f);

    [Tooltip("Ukuran lebar & tinggi icon warning.")]
    public Vector2 iconSize = new Vector2(48f, 48f);

    [Tooltip("Sembunyikan icon secara otomatis saat sensor Kinect sudah terhubung & streaming normal.")]
    public bool hideWhenConnected = true;

    [Tooltip("Warna dasar icon saat warning aktif (Default: Amber / Oranye Terang).")]
    public Color warningColor = new Color(1f, 0.72f, 0.05f, 1f);

    [Tooltip("Warna kedua untuk animasi kedip/denyut 'nyala' (Default: Merah-Oranye).")]
    public Color warningPulseColor = new Color(1f, 0.28f, 0.15f, 1f);

    [Tooltip("Kecepatan efek kedip/denyut 'nyala' saat sensor belum terdeteksi.")]
    [Range(0.5f, 10f)]
    public float pulseSpeed = 3.2f;

    [Header("3. Teks Pesan Hover Tooltip")]
    [Tooltip("Judul pop-up saat hover.")]
    public string tooltipTitle = "Xbox Kinect Belum Terdeteksi!";

    [TextArea(3, 6)]
    [Tooltip("Pesan instruksi detail saat hover.")]
    public string tooltipMessage = "Sensor Xbox Kinect belum siap atau tidak terhubung ke PC.\n\n• Periksa kabel data USB 3.0 ke PC\n• Periksa colokan power adaptor Kinect\n• Pastikan driver Kinect SDK v2 aktif";

    [Header("4. Live Hardware Status (Monitor)")]
    [SerializeField] private bool isKinectConnectedLive = false;
    [SerializeField] private string sensorPlatformName = "Unknown";
    [SerializeField] private bool hasReceivedAnyFrames = false;
    [SerializeField] private float secondsSinceLastFrame = 0f;

    [Header("5. Simulasi & Debug")]
    [Tooltip("Centang untuk mensimulasikan sensor terputus (untuk testing visual di Editor).")]
    public bool simulateDisconnected = false;

    // Internal runtime variables
    private bool isHovered = false;
    private float pulseTimer = 0f;
    private long lastDepthTime = 0;
    private long lastColorTime = 0;
    private float lastFrameReceivedRealtime = 0f;

    public static KinectStatusWarningUI Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        EnsureUIConstructed();
    }

    void OnEnable()
    {
        EnsureUIConstructed();
        lastFrameReceivedRealtime = Time.realtimeSinceStartup;
    }

    void Start()
    {
        EnsureUIConstructed();
        lastFrameReceivedRealtime = Time.realtimeSinceStartup;
        RefreshStatus(true);
    }

    void Update()
    {
        // 1. Periksa status fisik sensor Kinect realtime
        isKinectConnectedLive = CheckIsKinectDetected();

        // 2. Perbarui tampilan visibilitas icon
        UpdateIconVisibility(isKinectConnectedLive);

        // 3. Animasi 'nyala' (pulsating glow/color) saat sensor tidak terdeteksi
        if (!isKinectConnectedLive)
        {
            pulseTimer += Time.unscaledDeltaTime * pulseSpeed;
            float t = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;

            if (warningIconImage != null)
            {
                warningIconImage.color = Color.Lerp(warningColor, warningPulseColor, t);

                float scale = 1f + 0.06f * Mathf.Sin(pulseTimer * 1.5f);
                warningIconImage.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
        else
        {
            if (warningIconImage != null)
            {
                warningIconImage.transform.localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// Memeriksa secara mendalam apakah sensor Xbox Kinect fisik benar-benar terhubung,
    /// aktif, dan sedang mengirimkan frame data ke Unity.
    /// </summary>
    public bool CheckIsKinectDetected()
    {
        if (simulateDisconnected)
        {
            sensorPlatformName = "Simulated Disconnected";
            return false;
        }

        // =========================================================================
        // A. Cek di Play Mode melalui KinectManager dan Stream Frame Heartbeat
        // =========================================================================
        if (Application.isPlaying)
        {
            KinectManager km = KinectManager.Instance;
            if (km == null || !km.IsInitialized())
            {
                sensorPlatformName = "KinectManager Not Initialized";
                return false;
            }

            KinectInterop.SensorData sData = km.GetSensorData();
            if (sData == null)
            {
                sensorPlatformName = "SensorData is Null";
                return false;
            }

            // Jika fallback ke Dummy, berarti sensor asli tidak terdeteksi
            if (sData.sensorIntPlatform == KinectInterop.DepthSensorPlatform.DummyK2 ||
                sData.sensorIntPlatform == KinectInterop.DepthSensorPlatform.None)
            {
                sensorPlatformName = "Dummy / No Sensor (" + sData.sensorIntPlatform + ")";
                return false;
            }

            sensorPlatformName = sData.sensorIntPlatform.ToString();

            // Cek status IsAvailable langsung dari driver Windows.Kinect
            if (sData.sensorInterface is Kinect2Interface k2 && k2.kinectSensor != null)
            {
                if (!k2.kinectSensor.IsAvailable)
                {
                    sensorPlatformName = "Kinect2 Hardware Unavailable (Unplugged)";
                    return false;
                }
            }

            // Deteksi Heartbeat Frame (Sangat ampuh saat kabel dicabut tiba-tiba saat runtime)
            long curDepth = km.GetDepthFrameTime();
            long curColor = km.GetColorFrameTime();

            if (curDepth != lastDepthTime || curColor != lastColorTime)
            {
                lastDepthTime = curDepth;
                lastColorTime = curColor;
                lastFrameReceivedRealtime = Time.realtimeSinceStartup;
                hasReceivedAnyFrames = true;
            }

            secondsSinceLastFrame = Time.realtimeSinceStartup - lastFrameReceivedRealtime;

            // Jika aplikasi sudah berjalan > 2.0 detik tapi belum pernah ada 1 frame pun masuk:
            if (!hasReceivedAnyFrames && Time.timeSinceLevelLoad > 2.0f)
            {
                sensorPlatformName = "No Frames Received (Sensor Not Ready/Connected)";
                return false;
            }

            // Jika sebelumnya lancar, lalu kabel dicabut sehingga frame freeze > 1.2 detik:
            if (hasReceivedAnyFrames && secondsSinceLastFrame > 1.2f)
            {
                sensorPlatformName = $"Kinect Frame Stream Frozen ({secondsSinceLastFrame:F1}s ago)";
                return false;
            }

            return true;
        }

        // =========================================================================
        // B. Cek di Edit Mode (Scene View saat tidak play)
        // =========================================================================
        try
        {
            KinectSensor defaultSensor = KinectSensor.GetDefault();
            if (defaultSensor != null)
            {
                bool isAvail = defaultSensor.IsAvailable;
                sensorPlatformName = isAvail ? "Kinect2 Available (Edit Mode)" : "Kinect2 Unavailable (Edit Mode)";
                return isAvail;
            }
        }
        catch
        {
            // SDK not installed or failed to query
        }

        sensorPlatformName = "Kinect SDK Not Available";
        return false;
    }

    private void UpdateIconVisibility(bool isDetected)
    {
        // Pastikan CanvasGroup tersedia
        if (iconCanvasGroup == null && warningIconImage != null)
        {
            iconCanvasGroup = warningIconImage.GetComponent<CanvasGroup>();
            if (iconCanvasGroup == null) iconCanvasGroup = warningIconImage.gameObject.AddComponent<CanvasGroup>();
        }

        bool shouldShow = !isDetected || !hideWhenConnected;

        if (iconCanvasGroup != null)
        {
            iconCanvasGroup.alpha = shouldShow ? 1f : 0f;
            iconCanvasGroup.blocksRaycasts = shouldShow;
            iconCanvasGroup.interactable = shouldShow;
        }
        else if (warningIconImage != null)
        {
            warningIconImage.enabled = shouldShow;
        }

        // Jika sensor kembali terdeteksi, tutup tooltip
        if (isDetected && tooltipPanel != null && tooltipPanel.activeSelf)
        {
            tooltipPanel.SetActive(false);
            isHovered = false;
        }
    }

    public void RefreshStatus(bool forceUpdate = false)
    {
        bool isDetected = CheckIsKinectDetected();
        UpdateIconVisibility(isDetected);
        UpdateTooltipDisplay();
    }

    // =========================================================
    // Pointer Hover Handler (IPointerEnterHandler / Exit)
    // =========================================================
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CheckIsKinectDetected() || !hideWhenConnected)
        {
            isHovered = true;
            UpdateTooltipDisplay();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateTooltipDisplay();
    }

    private void UpdateTooltipDisplay()
    {
        if (tooltipPanel != null)
        {
            bool shouldShow = isHovered && (!CheckIsKinectDetected() || !hideWhenConnected);
            tooltipPanel.SetActive(shouldShow);

            if (shouldShow)
            {
                if (tooltipTitleText != null)
                {
                    tooltipTitleText.text = tooltipTitle;
                }
                if (tooltipBodyText != null)
                {
                    tooltipBodyText.text = tooltipMessage;
                }
            }
        }
    }

    [ContextMenu("Rebuild UI di Canvas")]
    public void EnsureUIConstructed()
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindAnyObjectByType<Canvas>();
        }

        if (targetCanvas == null) return;

        // Cari sprite warning jika belum diset
        if (warningSprite == null)
        {
#if UNITY_EDITOR
            warningSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/2D/warning_kinect.png");
#endif
            if (warningSprite == null)
            {
                Texture2D tex = CreateFallbackWarningTexture();
                warningSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        // 1. Root Icon Container (Pojok Kanan Atas)
        Transform existingIcon = targetCanvas.transform.Find("KinectWarning_Icon");
        GameObject iconObj = existingIcon != null ? existingIcon.gameObject : gameObject;

        RectTransform rtIcon = iconObj.GetComponent<RectTransform>();
        if (rtIcon == null) rtIcon = iconObj.AddComponent<RectTransform>();

        rtIcon.anchorMin = new Vector2(1f, 1f);
        rtIcon.anchorMax = new Vector2(1f, 1f);
        rtIcon.pivot = new Vector2(1f, 1f);
        rtIcon.anchoredPosition = anchoredPosition;
        rtIcon.sizeDelta = iconSize;

        warningIconImage = iconObj.GetComponent<Image>();
        if (warningIconImage == null) warningIconImage = iconObj.AddComponent<Image>();
        warningIconImage.sprite = warningSprite;
        warningIconImage.raycastTarget = true;
        warningIconImage.color = warningColor;

        iconCanvasGroup = iconObj.GetComponent<CanvasGroup>();
        if (iconCanvasGroup == null) iconCanvasGroup = iconObj.AddComponent<CanvasGroup>();

        // 2. Tooltip Panel
        Transform existingTooltip = iconObj.transform.Find("Tooltip_Panel");
        GameObject tipObj = existingTooltip != null ? existingTooltip.gameObject : null;

        if (tipObj == null)
        {
            tipObj = new GameObject("Tooltip_Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            tipObj.transform.SetParent(iconObj.transform, false);
        }

        RectTransform rtTip = tipObj.GetComponent<RectTransform>();
        rtTip.anchorMin = new Vector2(1f, 0f);
        rtTip.anchorMax = new Vector2(1f, 0f);
        rtTip.pivot = new Vector2(1f, 1f);
        rtTip.anchoredPosition = new Vector2(0f, -8f);
        rtTip.sizeDelta = new Vector2(320f, 130f);

        Image imgTip = tipObj.GetComponent<Image>();
        imgTip.color = new Color(0.10f, 0.12f, 0.15f, 0.96f);
        imgTip.raycastTarget = false;

        tooltipPanel = tipObj;
        CanvasGroup tipCg = tipObj.GetComponent<CanvasGroup>();
        if (tipCg != null) tipCg.blocksRaycasts = false;

        // 3. Tooltip Title Text
        Transform existingTitle = tipObj.transform.Find("Title_Text");
        GameObject titleObj = existingTitle != null ? existingTitle.gameObject : null;
        if (titleObj == null)
        {
            titleObj = new GameObject("Title_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(tipObj.transform, false);
        }

        RectTransform rtTitle = titleObj.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0f, 1f);
        rtTitle.anchorMax = new Vector2(1f, 1f);
        rtTitle.pivot = new Vector2(0f, 1f);
        rtTitle.anchoredPosition = new Vector2(12f, -10f);
        rtTitle.sizeDelta = new Vector2(-24f, 26f);

        tooltipTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        tooltipTitleText.text = tooltipTitle;
        tooltipTitleText.fontSize = 15f;
        tooltipTitleText.fontStyle = FontStyles.Bold;
        tooltipTitleText.color = new Color(1f, 0.82f, 0.2f, 1f);
        tooltipTitleText.alignment = TextAlignmentOptions.TopLeft;
        tooltipTitleText.raycastTarget = false;

        // 4. Tooltip Body Text
        Transform existingBody = tipObj.transform.Find("Body_Text");
        GameObject bodyObj = existingBody != null ? existingBody.gameObject : null;
        if (bodyObj == null)
        {
            bodyObj = new GameObject("Body_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObj.transform.SetParent(tipObj.transform, false);
        }

        RectTransform rtBody = bodyObj.GetComponent<RectTransform>();
        rtBody.anchorMin = new Vector2(0f, 0f);
        rtBody.anchorMax = new Vector2(1f, 1f);
        rtBody.pivot = new Vector2(0f, 1f);
        rtBody.anchoredPosition = new Vector2(12f, -38f);
        rtBody.sizeDelta = new Vector2(-24f, -46f);

        tooltipBodyText = bodyObj.GetComponent<TextMeshProUGUI>();
        tooltipBodyText.text = tooltipMessage;
        tooltipBodyText.fontSize = 11.5f;
        tooltipBodyText.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        tooltipBodyText.alignment = TextAlignmentOptions.TopLeft;
        tooltipBodyText.textWrappingMode = TextWrappingModes.Normal;
        tooltipBodyText.raycastTarget = false;

        tooltipPanel.SetActive(false);
    }

    private Texture2D CreateFallbackWarningTexture()
    {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        Color warn = new Color(1f, 0.7f, 0f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float normY = (float)y / h;
                float halfWidth = normY * 0.5f * w;
                float midX = w * 0.5f;
                if (x >= midX - halfWidth && x <= midX + halfWidth && y >= 6 && y <= 58)
                {
                    tex.SetPixel(x, y, warn);
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    void OnValidate()
    {
        if (warningIconImage != null)
        {
            RectTransform rt = warningIconImage.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = iconSize;
            }
        }
    }
}
