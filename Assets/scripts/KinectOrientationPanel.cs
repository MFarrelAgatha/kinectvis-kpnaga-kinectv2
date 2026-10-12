using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// KinectOrientationPanel
/// Panel UI Canvas (uGUI) di Kanan Tengah layar untuk mengatur orientasi posisi sensor Kinect
/// (Depan, Belakang, Kiri, Kanan) dan toggle Invert Maju-Mundur / Mirror Kiri-Kanan secara realtime.
/// Otomatis mati saat GameObject mati, dan dapat digenerate langsung ke dalam Canvas.
/// </summary>
[DisallowMultipleComponent]
public class KinectOrientationPanel : MonoBehaviour
{
    public enum SensorDirection
    {
        Depan_Atas = 0,       // 0 deg (Sensor di depan player / arah Z+)
        Kanan = 90,           // 90 deg (Sensor di samping kanan player)
        Belakang_Bawah = 180, // 180 deg (Sensor di belakang player / arah Z-)
        Kiri = 270            // 270 deg (Sensor di samping kiri player)
    }

    [Header("Target Controller")]
    [Tooltip("Referensi ke AvatarController. Jika kosong, otomatis mencari di scene.")]
    public AvatarController avatarController;

    [Header("Status Orientasi Kinect")]
    [Tooltip("Arah penempatan sensor Kinect fisik di ruangan.")]
    public SensorDirection currentDirection = SensorDirection.Depan_Atas;

    [Tooltip("Membalik arah sumbu Z (maju/mundur). Nyalakan jika saat mendekat ke kamera Kinect, karakter malah berjalan menjauh.")]
    public bool invertForwardBackward = false;

    [Tooltip("Membalik arah sumbu X (kiri/kanan).")]
    public bool invertLeftRight = false;

    [Header("Pengaturan Tampilan Saat Mulai")]
    [Tooltip("Tampilkan panel saat game dimulai.")]
    public bool showPanelOnStart = true;

    [Tooltip("Jika true, panel dimulai dalam keadaan MINIMIZE (hanya tab kecil di kanan). Jika false, panel langsung terbuka lebar.")]
    public bool startMinimized = false;

    [Header("Pengaturan Tampilan Runtime")]
    [Tooltip("Tampilkan / sembunyikan panel.")]
    public bool showPanel = true;

    [Tooltip("Status saat ini: apakah panel sedang dalam mode minimize (hanya tombol tab kecil di tepi kanan)?")]
    public bool isMinimized = false;

    [Tooltip("Hotkey keyboard untuk membuka/menutup panel (Default: F2).")]
    public KeyCode toggleHotkey = KeyCode.F2;

    [Header("Pengaturan Warna Button & Panel (Public)")]
    [Tooltip("Warna background tombol arah yang sedang AKTIF (Default: Neon Teal / Emerald).")]
    public Color colorActiveBtnBg = new Color(0.0f, 0.88f, 0.72f, 1f);

    [Tooltip("Warna teks tombol arah yang sedang AKTIF (Default: Dark Navy).")]
    public Color colorActiveBtnText = new Color(0.05f, 0.08f, 0.12f, 1f);

    [Tooltip("Warna background tombol normal / tidak aktif (Default: Dark Slate).")]
    public Color colorNormalBtnBg = new Color(0.14f, 0.20f, 0.28f, 0.95f);

    [Tooltip("Warna teks tombol normal / tidak aktif (Default: Putih).")]
    public Color colorNormalBtnText = Color.white;

    [Tooltip("Warna background tombol status Terbalik / Peringatan (Default: Coral Red).")]
    public Color colorWarnBtnBg = new Color(0.85f, 0.25f, 0.25f, 0.95f);

    [Tooltip("Warna background tombol status Normal / Aman (Default: Forest Green).")]
    public Color colorSafeBtnBg = new Color(0.20f, 0.65f, 0.35f, 0.95f);

    [Tooltip("Warna background tombol aksi khusus seperti Kalibrasi & Tab Floating (Default: Royal Blue).")]
    public Color colorBlueBtnBg = new Color(0.12f, 0.45f, 0.85f, 0.95f);

    [Tooltip("Warna background tombol minimize [-] (Default: Dark Slate Gray).")]
    public Color colorMinimizeBtnBg = new Color(0.2f, 0.28f, 0.38f, 0.9f);

    [Tooltip("Warna background kartu panel utama.")]
    public Color colorPanelCardBg = new Color(0.08f, 0.12f, 0.18f, 0.94f);

    [Tooltip("Warna header bar kartu panel.")]
    public Color colorHeaderBg = new Color(0.12f, 0.18f, 0.26f, 0.98f);

    [Header("Referensi UI Canvas (Auto-Generated)")]
    public Canvas targetCanvas;
    public GameObject panelContainer;
    public GameObject panelCard;
    public GameObject floatingTab;

    [Header("Komponen UI Buttons")]
    public Button btnDepan;
    public Button btnBelakang;
    public Button btnKiri;
    public Button btnKanan;
    public Button btnInvertZ;
    public Button btnInvertX;
    public Button btnRecalibrate;
    public Button btnMinimize;
    public Button btnFloatingOpen;

    [Header("Komponen UI Text (TMP)")]
    public TMP_Text textHeaderTitle;
    public TMP_Text textBtnDepan;
    public TMP_Text textBtnBelakang;
    public TMP_Text textBtnKiri;
    public TMP_Text textBtnKanan;
    public TMP_Text textBtnInvertZ;
    public TMP_Text textBtnInvertX;
    public TMP_Text textStatusSummary;

    void Awake()
    {
        FindController();
        EnsureUIPanelExists();
    }

    void Start()
    {
        // Terapkan konfigurasi start minimize / show panel
        showPanel = showPanelOnStart;
        isMinimized = startMinimized;

        FindController();
        EnsureUIPanelExists();
        ApplySettingsToController();
        UpdateUIElements();
    }

    void OnEnable()
    {
        FindController();
        if (panelContainer != null)
        {
            panelContainer.SetActive(showPanel);
        }
        UpdateUIElements();
    }

    void OnDisable()
    {
        // Pastikan UI panel mati saat GameObject ini mati / disabled
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            showPanel = showPanelOnStart;
            isMinimized = startMinimized;
        }
        UpdateUIElements();
    }

    void Update()
    {
        if (!enabled || !gameObject.activeInHierarchy) return;

        if (Input.GetKeyDown(toggleHotkey))
        {
            showPanel = !showPanel;
            if (panelContainer != null)
            {
                panelContainer.SetActive(showPanel);
            }
            UpdateUIElements();
        }

        if (avatarController == null)
        {
            FindController();
        }
    }

    private void FindController()
    {
        if (avatarController == null)
        {
            avatarController = FindAnyObjectByType<AvatarController>();
        }
    }

    public void SetDirection(SensorDirection dir)
    {
        currentDirection = dir;
        ApplySettingsToController();
        UpdateUIElements();
    }

    public void ToggleInvertZ()
    {
        invertForwardBackward = !invertForwardBackward;
        ApplySettingsToController();
        UpdateUIElements();
    }

    public void ToggleInvertX()
    {
        invertLeftRight = !invertLeftRight;
        ApplySettingsToController();
        UpdateUIElements();
    }

    public void Recalibrate()
    {
        if (avatarController != null)
        {
            avatarController.RecalibrateOffset();
            Debug.Log("[KinectOrientation] Offset avatar dikalibrasi ulang.");
        }
        UpdateUIElements();
    }

    public void ToggleMinimize(bool minimize)
    {
        isMinimized = minimize;
        UpdateUIElements();
    }

    private void ApplySettingsToController()
    {
        if (avatarController == null) return;

        avatarController.sensorPlacementAngle = (float)currentDirection;
        avatarController.invertZMovement = invertForwardBackward;
        avatarController.invertXMovement = invertLeftRight;
        avatarController.RecalibrateOffset();
    }

    /// <summary>
    /// Memperbarui visual button, warna aktif, dan teks status pada UI Canvas.
    /// </summary>
    public void UpdateUIElements()
    {
        if (panelContainer == null) return;

        bool isSelfActive = enabled && gameObject.activeInHierarchy && showPanel;
        panelContainer.SetActive(isSelfActive);

        if (!isSelfActive) return;

        if (panelCard != null)
        {
            panelCard.SetActive(!isMinimized);
            Image imgCard = panelCard.GetComponent<Image>();
            if (imgCard != null) imgCard.color = colorPanelCardBg;

            Transform header = panelCard.transform.Find("Header_Bar");
            if (header != null)
            {
                Image imgHeader = header.GetComponent<Image>();
                if (imgHeader != null) imgHeader.color = colorHeaderBg;
            }
        }

        if (floatingTab != null)
        {
            floatingTab.SetActive(isMinimized);
            Image imgTab = floatingTab.GetComponent<Image>();
            if (imgTab != null) imgTab.color = colorBlueBtnBg;
        }

        if (btnMinimize != null)
        {
            Image imgMin = btnMinimize.GetComponent<Image>();
            if (imgMin != null) imgMin.color = colorMinimizeBtnBg;
        }

        if (!isMinimized)
        {
            // Update tombol 4 arah
            UpdateButtonVisual(btnDepan, textBtnDepan, currentDirection == SensorDirection.Depan_Atas, "DEPAN (0 DEG)");
            UpdateButtonVisual(btnBelakang, textBtnBelakang, currentDirection == SensorDirection.Belakang_Bawah, "BELAKANG (180 DEG)");
            UpdateButtonVisual(btnKiri, textBtnKiri, currentDirection == SensorDirection.Kiri, "KIRI (270 DEG)");
            UpdateButtonVisual(btnKanan, textBtnKanan, currentDirection == SensorDirection.Kanan, "KANAN (90 DEG)");

            // Update tombol Invert Z (Maju/Mundur)
            if (btnInvertZ != null)
            {
                Image img = btnInvertZ.GetComponent<Image>();
                if (img != null) img.color = invertForwardBackward ? colorWarnBtnBg : colorSafeBtnBg;
            }
            if (textBtnInvertZ != null)
            {
                textBtnInvertZ.text = invertForwardBackward
                    ? "Maju/Mundur: [TERBALIK]"
                    : "Maju/Mundur: [NORMAL]";
            }

            // Update tombol Invert X (Kiri/Kanan)
            if (btnInvertX != null)
            {
                Image img = btnInvertX.GetComponent<Image>();
                if (img != null) img.color = invertLeftRight ? colorWarnBtnBg : colorSafeBtnBg;
            }
            if (textBtnInvertX != null)
            {
                textBtnInvertX.text = invertLeftRight
                    ? "Kiri/Kanan: [MIRROR]"
                    : "Kiri/Kanan: [NORMAL]";
            }

            // Update tombol Re-Kalibrasi
            if (btnRecalibrate != null)
            {
                Image img = btnRecalibrate.GetComponent<Image>();
                if (img != null) img.color = colorBlueBtnBg;
            }

            // Update summary status text
            if (textStatusSummary != null)
            {
                string arahStr = currentDirection.ToString().Replace("_", " / ").ToUpper();
                string zStatus = invertForwardBackward ? "TERBALIK" : "NORMAL";
                string xStatus = invertLeftRight ? "MIRROR" : "NORMAL";
                textStatusSummary.text = $"Posisi: {arahStr} | Z: {zStatus} | X: {xStatus}";
            }
        }
    }

    private void UpdateButtonVisual(Button btn, TMP_Text txt, bool isActive, string label)
    {
        if (btn != null)
        {
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? colorActiveBtnBg : colorNormalBtnBg;
            }
        }
        if (txt != null)
        {
            txt.text = isActive ? $"<b>[ {label} ]</b>" : label;
            txt.color = isActive ? colorActiveBtnText : colorNormalBtnText;
        }
    }

    /// <summary>
    /// Memastikan objek UI Panel ada di dalam Canvas. Jika belum ada, otomatis dibangun.
    /// </summary>
    public void EnsureUIPanelExists()
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindAnyObjectByType<Canvas>();
        }
        if (targetCanvas == null) return;

        Transform existing = targetCanvas.transform.Find("Panel Posisi Kinect");
        if (existing != null)
        {
            panelContainer = existing.gameObject;
            HookExistingUIReferences(panelContainer);
            return;
        }

        // Buat panel baru secara programmatic
        BuildUIPanelUnderCanvas();
    }

    /// <summary>
    /// Membangun struktur UI Panel modern di Canvas (Right-Middle anchor, no emojis).
    /// </summary>
    public void BuildUIPanelUnderCanvas()
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindAnyObjectByType<Canvas>();
        }
        if (targetCanvas == null)
        {
            Debug.LogError("[KinectOrientationPanel] Tidak ada Canvas di scene!");
            return;
        }

        Transform existing = targetCanvas.transform.Find("Panel Posisi Kinect");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        // 1. Root Container
        panelContainer = new GameObject("Panel Posisi Kinect", typeof(RectTransform));
        panelContainer.transform.SetParent(targetCanvas.transform, false);
        RectTransform rtContainer = panelContainer.GetComponent<RectTransform>();
        rtContainer.anchorMin = new Vector2(1f, 0.5f);
        rtContainer.anchorMax = new Vector2(1f, 0.5f);
        rtContainer.pivot = new Vector2(1f, 0.5f);
        rtContainer.anchoredPosition = new Vector2(-20f, 0f);
        rtContainer.sizeDelta = new Vector2(280f, 380f);

        // 2. Main Card Panel
        panelCard = new GameObject("Card_Panel", typeof(RectTransform), typeof(Image));
        panelCard.transform.SetParent(panelContainer.transform, false);
        RectTransform rtCard = panelCard.GetComponent<RectTransform>();
        rtCard.anchorMin = Vector2.zero;
        rtCard.anchorMax = Vector2.one;
        rtCard.sizeDelta = Vector2.zero;
        Image imgCard = panelCard.GetComponent<Image>();
        imgCard.color = colorPanelCardBg;

        // 2.1 Header Bar
        GameObject headerObj = new GameObject("Header_Bar", typeof(RectTransform), typeof(Image));
        headerObj.transform.SetParent(panelCard.transform, false);
        RectTransform rtHeader = headerObj.GetComponent<RectTransform>();
        rtHeader.anchorMin = new Vector2(0f, 1f);
        rtHeader.anchorMax = new Vector2(1f, 1f);
        rtHeader.pivot = new Vector2(0.5f, 1f);
        rtHeader.anchoredPosition = Vector2.zero;
        rtHeader.sizeDelta = new Vector2(0f, 38f);
        headerObj.GetComponent<Image>().color = colorHeaderBg;

        // Header Title Text
        GameObject headerTitleObj = new GameObject("Title_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerTitleObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtTitle = headerTitleObj.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0f, 0f);
        rtTitle.anchorMax = new Vector2(0.8f, 1f);
        rtTitle.anchoredPosition = new Vector2(12f, 0f);
        textHeaderTitle = headerTitleObj.GetComponent<TextMeshProUGUI>();
        textHeaderTitle.font = font;
        textHeaderTitle.text = "<b>POSISI KINECT</b>";
        textHeaderTitle.fontSize = 14;
        textHeaderTitle.color = new Color(0.0f, 0.9f, 1.0f);
        textHeaderTitle.alignment = TextAlignmentOptions.MidlineLeft;

        // Header Minimize Button
        GameObject minBtnObj = new GameObject("Btn_Minimize", typeof(RectTransform), typeof(Image), typeof(Button));
        minBtnObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtMinBtn = minBtnObj.GetComponent<RectTransform>();
        rtMinBtn.anchorMin = new Vector2(1f, 0.5f);
        rtMinBtn.anchorMax = new Vector2(1f, 0.5f);
        rtMinBtn.pivot = new Vector2(1f, 0.5f);
        rtMinBtn.anchoredPosition = new Vector2(-8f, 0f);
        rtMinBtn.sizeDelta = new Vector2(30f, 24f);
        minBtnObj.GetComponent<Image>().color = colorMinimizeBtnBg;
        btnMinimize = minBtnObj.GetComponent<Button>();

        GameObject minTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        minTxtObj.transform.SetParent(minBtnObj.transform, false);
        RectTransform rtMinTxt = minTxtObj.GetComponent<RectTransform>();
        rtMinTxt.anchorMin = Vector2.zero;
        rtMinTxt.anchorMax = Vector2.one;
        rtMinTxt.sizeDelta = Vector2.zero;
        TMP_Text tMin = minTxtObj.GetComponent<TextMeshProUGUI>();
        tMin.font = font;
        tMin.text = "[-]";
        tMin.fontSize = 13;
        tMin.alignment = TextAlignmentOptions.Center;
        tMin.color = Color.white;

        // 2.2 Content Area Container
        GameObject contentArea = new GameObject("Content_Area", typeof(RectTransform));
        contentArea.transform.SetParent(panelCard.transform, false);
        RectTransform rtContent = contentArea.GetComponent<RectTransform>();
        rtContent.anchorMin = Vector2.zero;
        rtContent.anchorMax = Vector2.one;
        rtContent.offsetMin = new Vector2(10f, 10f);
        rtContent.offsetMax = new Vector2(-10f, -44f);

        // Section Label 1: Arah Sensor Fisik
        CreateLabel(contentArea.transform, "LABEL ARAH SENSOR FISIK:", 10f, -6f, 260f, 18f, new Color(0.6f, 0.8f, 1f));

        // Tombol DEPAN (0 deg)
        btnDepan = CreateButton(contentArea.transform, "Btn_Depan", "DEPAN (0 DEG)", 0f, -28f, 260f, 32f, out textBtnDepan);

        // Baris KIRI (270 deg) & KANAN (90 deg)
        btnKiri = CreateButton(contentArea.transform, "Btn_Kiri", "KIRI (270 DEG)", -68f, -66f, 124f, 32f, out textBtnKiri);
        btnKanan = CreateButton(contentArea.transform, "Btn_Kanan", "KANAN (90 DEG)", 68f, -66f, 124f, 32f, out textBtnKanan);

        // Tombol BELAKANG (180 deg)
        btnBelakang = CreateButton(contentArea.transform, "Btn_Belakang", "BELAKANG (180 DEG)", 0f, -104f, 260f, 32f, out textBtnBelakang);

        // Section Label 2: Kontrol Sumbu (Invert)
        CreateLabel(contentArea.transform, "KONTROL SUMBU (INVERT):", 10f, -148f, 260f, 18f, new Color(1f, 0.85f, 0.4f));

        // Tombol Invert Z
        btnInvertZ = CreateButton(contentArea.transform, "Btn_InvertZ", "Maju/Mundur: [NORMAL]", 0f, -170f, 260f, 30f, out textBtnInvertZ);

        // Tombol Invert X
        btnInvertX = CreateButton(contentArea.transform, "Btn_InvertX", "Kiri/Kanan: [NORMAL]", 0f, -206f, 260f, 30f, out textBtnInvertX);

        // Tombol Re-Kalibrasi Posisi Avatar
        TMP_Text txtRecal;
        btnRecalibrate = CreateButton(contentArea.transform, "Btn_Recalibrate", "Re-Kalibrasi Posisi Avatar", 0f, -246f, 260f, 30f, out txtRecal);
        if (btnRecalibrate != null) btnRecalibrate.GetComponent<Image>().color = colorBlueBtnBg;

        // Status Summary Text
        GameObject statusObj = new GameObject("Text_Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusObj.transform.SetParent(contentArea.transform, false);
        RectTransform rtStatus = statusObj.GetComponent<RectTransform>();
        rtStatus.anchorMin = new Vector2(0f, 0f);
        rtStatus.anchorMax = new Vector2(1f, 0f);
        rtStatus.pivot = new Vector2(0.5f, 0f);
        rtStatus.anchoredPosition = new Vector2(0f, 4f);
        rtStatus.sizeDelta = new Vector2(0f, 24f);
        textStatusSummary = statusObj.GetComponent<TextMeshProUGUI>();
        textStatusSummary.font = font;
        textStatusSummary.fontSize = 10;
        textStatusSummary.alignment = TextAlignmentOptions.Center;
        textStatusSummary.color = new Color(0.75f, 0.85f, 0.95f);

        // 3. Floating Tab (ketika Minimized)
        floatingTab = new GameObject("Tab_Minimized", typeof(RectTransform), typeof(Image), typeof(Button));
        floatingTab.transform.SetParent(panelContainer.transform, false);
        RectTransform rtTab = floatingTab.GetComponent<RectTransform>();
        rtTab.anchorMin = new Vector2(1f, 0.5f);
        rtTab.anchorMax = new Vector2(1f, 0.5f);
        rtTab.pivot = new Vector2(1f, 0.5f);
        rtTab.anchoredPosition = Vector2.zero;
        rtTab.sizeDelta = new Vector2(130f, 38f);
        floatingTab.GetComponent<Image>().color = colorBlueBtnBg;
        btnFloatingOpen = floatingTab.GetComponent<Button>();

        GameObject tabTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        tabTxtObj.transform.SetParent(floatingTab.transform, false);
        RectTransform rtTabTxt = tabTxtObj.GetComponent<RectTransform>();
        rtTabTxt.anchorMin = Vector2.zero;
        rtTabTxt.anchorMax = Vector2.one;
        rtTabTxt.sizeDelta = Vector2.zero;
        TMP_Text tTab = tabTxtObj.GetComponent<TextMeshProUGUI>();
        tTab.font = font;
        tTab.text = "<b>[ Posisi Kinect ]</b>";
        tTab.fontSize = 11;
        tTab.alignment = TextAlignmentOptions.Center;
        tTab.color = Color.white;

        // 4. Hook Listeners
        HookButtonEvents();
        UpdateUIElements();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        if (targetCanvas != null) EditorUtility.SetDirty(targetCanvas);
#endif
    }

    private Button CreateButton(Transform parent, string name, string label, float posX, float posY, float width, float height, out TMP_Text textComponent)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(posX, posY);
        rt.sizeDelta = new Vector2(width, height);
        btnObj.GetComponent<Image>().color = colorNormalBtnBg;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.sizeDelta = Vector2.zero;
        textComponent = txtObj.GetComponent<TextMeshProUGUI>();
        textComponent.font = TMP_Settings.defaultFontAsset;
        textComponent.text = label;
        textComponent.fontSize = 11;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = colorNormalBtnText;

        return btnObj.GetComponent<Button>();
    }

    private void CreateLabel(Transform parent, string text, float posX, float posY, float width, float height, Color color)
    {
        GameObject lblObj = new GameObject("Label_" + text.Replace(" ", "_"), typeof(RectTransform), typeof(TextMeshProUGUI));
        lblObj.transform.SetParent(parent, false);
        RectTransform rt = lblObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(posX, posY);
        rt.sizeDelta = new Vector2(width, height);
        TMP_Text t = lblObj.GetComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = $"<b>{text}</b>";
        t.fontSize = 10;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.color = color;
    }

    private void HookButtonEvents()
    {
        if (btnDepan != null)
        {
            btnDepan.onClick.RemoveAllListeners();
            btnDepan.onClick.AddListener(() => SetDirection(SensorDirection.Depan_Atas));
        }
        if (btnBelakang != null)
        {
            btnBelakang.onClick.RemoveAllListeners();
            btnBelakang.onClick.AddListener(() => SetDirection(SensorDirection.Belakang_Bawah));
        }
        if (btnKiri != null)
        {
            btnKiri.onClick.RemoveAllListeners();
            btnKiri.onClick.AddListener(() => SetDirection(SensorDirection.Kiri));
        }
        if (btnKanan != null)
        {
            btnKanan.onClick.RemoveAllListeners();
            btnKanan.onClick.AddListener(() => SetDirection(SensorDirection.Kanan));
        }
        if (btnInvertZ != null)
        {
            btnInvertZ.onClick.RemoveAllListeners();
            btnInvertZ.onClick.AddListener(ToggleInvertZ);
        }
        if (btnInvertX != null)
        {
            btnInvertX.onClick.RemoveAllListeners();
            btnInvertX.onClick.AddListener(ToggleInvertX);
        }
        if (btnRecalibrate != null)
        {
            btnRecalibrate.onClick.RemoveAllListeners();
            btnRecalibrate.onClick.AddListener(Recalibrate);
        }
        if (btnMinimize != null)
        {
            btnMinimize.onClick.RemoveAllListeners();
            btnMinimize.onClick.AddListener(() => ToggleMinimize(true));
        }
        if (btnFloatingOpen != null)
        {
            btnFloatingOpen.onClick.RemoveAllListeners();
            btnFloatingOpen.onClick.AddListener(() => ToggleMinimize(false));
        }
    }

    private void HookExistingUIReferences(GameObject root)
    {
        panelCard = root.transform.Find("Card_Panel")?.gameObject;
        floatingTab = root.transform.Find("Tab_Minimized")?.gameObject;

        if (panelCard != null)
        {
            Transform header = panelCard.transform.Find("Header_Bar");
            if (header != null)
            {
                btnMinimize = header.Find("Btn_Minimize")?.GetComponent<Button>();
                textHeaderTitle = header.Find("Title_Text")?.GetComponent<TMP_Text>();
            }

            Transform content = panelCard.transform.Find("Content_Area");
            if (content != null)
            {
                btnDepan = content.Find("Btn_Depan")?.GetComponent<Button>();
                textBtnDepan = btnDepan?.GetComponentInChildren<TMP_Text>();

                btnBelakang = content.Find("Btn_Belakang")?.GetComponent<Button>();
                textBtnBelakang = btnBelakang?.GetComponentInChildren<TMP_Text>();

                btnKiri = content.Find("Btn_Kiri")?.GetComponent<Button>();
                textBtnKiri = btnKiri?.GetComponentInChildren<TMP_Text>();

                btnKanan = content.Find("Btn_Kanan")?.GetComponent<Button>();
                textBtnKanan = btnKanan?.GetComponentInChildren<TMP_Text>();

                btnInvertZ = content.Find("Btn_InvertZ")?.GetComponent<Button>();
                textBtnInvertZ = btnInvertZ?.GetComponentInChildren<TMP_Text>();

                btnInvertX = content.Find("Btn_InvertX")?.GetComponent<Button>();
                textBtnInvertX = btnInvertX?.GetComponentInChildren<TMP_Text>();

                btnRecalibrate = content.Find("Btn_Recalibrate")?.GetComponent<Button>();
                textStatusSummary = content.Find("Text_Status")?.GetComponent<TMP_Text>();
            }
        }

        if (floatingTab != null)
        {
            btnFloatingOpen = floatingTab.GetComponent<Button>();
        }

        HookButtonEvents();
        UpdateUIElements();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(KinectOrientationPanel))]
public class KinectOrientationPanelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        KinectOrientationPanel panel = (KinectOrientationPanel)target;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Panel Canvas Generator", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.1f, 0.85f, 0.7f);
        if (GUILayout.Button("Generate / Refresh UI Panel di Canvas", GUILayout.Height(34)))
        {
            panel.BuildUIPanelUnderCanvas();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif
