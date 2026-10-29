#if UNITY_EDITOR

using UnityEditor;

using UnityEngine;



[CustomEditor(typeof(PoseCalibrationManager))]

public class PoseCalibrationManagerEditor : Editor

{

    private bool showMetricsFold = true;

    private bool showThresholdsFold = false;

    private string lastRecordedFeedback = "";

    private double lastRecordedTime = -100;



    public override void OnInspectorGUI()

    {

        PoseCalibrationManager mgr = (PoseCalibrationManager)target;



        if (mgr.referencePoseAvatar != null && !Application.isPlaying)

        {

            mgr.DisableDummyAvatarControllers();

            mgr.RefreshDummyVisualizer();

            mgr.UpdateLiveMetrics();

            mgr.EvaluateAllScores();

        }



        serializedObject.Update();



        EditorGUILayout.Space(6);

        DrawHeaderBanner(mgr);

        EditorGUILayout.Space(8);



        // 1. Reference Avatar Slot

        EditorGUILayout.LabelField("1. Target Avatar (Dummy Referensi)", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("referencePoseAvatar"), new GUIContent("Reference Pose Avatar"));

        if (mgr.referencePoseAvatar == null)

        {

            EditorGUILayout.HelpBox("Tarik GameObject avatar dummy (misal duplikasi U_Character) ke sini agar avatar asli tidak terganggu saat bereksperimen!", MessageType.Info);

        }

        else

        {

            EditorGUILayout.HelpBox($"Target terhubung: '{mgr.referencePoseAvatar.name}'. Avatar asli aman dari perubahan langsung.", MessageType.None);

        }



        EditorGUILayout.PropertyField(serializedObject.FindProperty("tungkuTracker"), new GUIContent("Player Tungku Tracker"));



        EditorGUILayout.Space(8);



        // 2. Winner Banner & Accuracy Breakdown

        DrawAccuracySection(mgr);



        EditorGUILayout.Space(10);



        // 3. Showcase 100% Buttons & Record

        DrawShowcaseAndRecordSection(mgr);



        EditorGUILayout.Space(10);



        // 4. Live Metrics Foldout

        showMetricsFold = EditorGUILayout.Foldout(showMetricsFold, "Pantauan Metriks Realtime (Dummy Avatar)", true, EditorStyles.foldoutHeader);

        if (showMetricsFold)

        {

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"Tinggi Pinggul (Hips): {mgr.LiveHipHeight:F2} m");

            EditorGUILayout.LabelField($"Sudut Tekukan Lutut: {mgr.LiveKneeAngle:F1}°");

            EditorGUILayout.LabelField($"Tinggi Lutut dari Tanah: {mgr.LiveKneeHeight:F2} m");

            EditorGUILayout.LabelField($"Lebar Sebaran Lutut (Sila): {mgr.LiveKneeSpread:F2} m");

            EditorGUILayout.LabelField($"Asimetri Ankle/Kaki (Emok): {mgr.LiveAnkleAsymmetry:F2} m");

            EditorGUI.indentLevel--;

        }



        EditorGUILayout.Space(8);



        // 5. Calibrated Thresholds Foldout

        showThresholdsFold = EditorGUILayout.Foldout(showThresholdsFold, "Pengaturan Ambang Batas Acuan (Thresholds)", true, EditorStyles.foldoutHeader);

        if (showThresholdsFold)

        {

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("squatHipHeight"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("squatKneeAngle"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("kneelingKneeHeight"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("kneelingHipHeight"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("silaHipHeight"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("silaKneeSpread"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("emokHipHeight"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("emokAnkleAsymmetry"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("chairHipHeight"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("chairKneeAngle"));

            EditorGUI.indentLevel--;

        }



        EditorGUILayout.Space(12);



        // 6. Action Button: Sync to Tracker

        GUI.backgroundColor = new Color(0.15f, 0.85f, 0.5f);

        if (GUILayout.Button(">> SYNC HASIL KALIBRASI KE PLAYER TRACKER <<", GUILayout.Height(38)))

        {

            mgr.SyncThresholdsToTracker();

        }

        GUI.backgroundColor = Color.white;



        EditorGUILayout.Space(6);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("showOnScreenGUI"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("toggleGuiHotkey"));



        serializedObject.ApplyModifiedProperties();



        // Repaint Scene & Inspector continuously in Editor if playing or reference exists

        if (mgr.referencePoseAvatar != null && !Application.isPlaying)

        {

            mgr.DisableDummyAvatarControllers();

            mgr.UpdateLiveMetrics();

            mgr.EvaluateAllScores();

        }

    }



    private void DrawHeaderBanner(PoseCalibrationManager mgr)

    {

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)

        {

            fontSize = 13,

            alignment = TextAnchor.MiddleCenter

        };



        EditorGUILayout.LabelField("POSE CALIBRATION & SHOWCASE SYSTEM", titleStyle);

    }



    private void DrawAccuracySection(PoseCalibrationManager mgr)

    {

        EditorGUILayout.LabelField("2. Evaluasi Akurasi Seluruh Label & Filter Postur Aktif", EditorStyles.boldLabel);



        // Big Winner Box menggunakan warna panel yang sesuai

        bool hasActiveWinner = mgr.TopAccuracyPercent > 0.01f && mgr.DetectedPostureLabel != "Semua Nonaktif";

        Color winnerColor = hasActiveWinner ? mgr.GetPostureColor(mgr.DetectedPosture) : new Color(0.25f, 0.25f, 0.25f, 0.9f);

        GUI.backgroundColor = winnerColor;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUIStyle winnerStyle = new GUIStyle(EditorStyles.largeLabel)

        {

            fontSize = 14,

            fontStyle = FontStyle.Bold,

            alignment = TextAnchor.MiddleCenter,

            normal = { textColor = Color.white }

        };

        string bannerText = hasActiveWinner 

            ? $"TERDETEKSI: {mgr.DetectedPostureLabel.ToUpper()} ({mgr.TopAccuracyPercent:F1}%)"

            : "SEMUA LABEL DUDUK/POSTUR NONAKTIF";

        EditorGUILayout.LabelField(bannerText, winnerStyle);

        EditorGUILayout.EndVertical();

        GUI.backgroundColor = Color.white;



        EditorGUILayout.Space(4);



        // Checkbox Boolean per Label (Direct Bools)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Checkbox Boolean per Label (Centang / Uncheck untuk Mengaktifkan):", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        DrawLabelBoolToggle(mgr, PostureType.Standing, "Berdiri");
        DrawLabelBoolToggle(mgr, PostureType.Squatting, "Jongkok");
        DrawLabelBoolToggle(mgr, PostureType.Kneeling, "Berlutut");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawLabelBoolToggle(mgr, PostureType.SittingCrossLegged, "Duduk Sila");
        DrawLabelBoolToggle(mgr, PostureType.SittingEmok, "Duduk Emok");
        DrawLabelBoolToggle(mgr, PostureType.SittingChair, "Duduk Normal");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // Quick Filter Buttons (Untuk mempermudah user mengisolasi pose duduk)

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("✓ Aktifkan Semua", EditorStyles.miniButtonLeft, GUILayout.Height(22)))

        {

            Undo.RecordObject(mgr, "Enable All Postures");

            mgr.EnableAllPostures(true);

            serializedObject.Update();

            mgr.EvaluateAllScores();

            EditorUtility.SetDirty(mgr);

        }

        if (GUILayout.Button("Hanya Duduk Normal", EditorStyles.miniButtonMid, GUILayout.Height(22)))

        {

            Undo.RecordObject(mgr, "Filter Duduk Normal Only");

            mgr.EnableOnlySittingChair();

            serializedObject.Update();

            mgr.EvaluateAllScores();

            EditorUtility.SetDirty(mgr);

        }

        if (GUILayout.Button("Matikan Semua Duduk", EditorStyles.miniButtonMid, GUILayout.Height(22)))

        {

            Undo.RecordObject(mgr, "Disable All Sitting");

            mgr.DisableAllSitting();

            serializedObject.Update();

            mgr.EvaluateAllScores();

            EditorUtility.SetDirty(mgr);

        }

        if (GUILayout.Button("✗ Matikan Semua", EditorStyles.miniButtonRight, GUILayout.Height(22)))

        {

            Undo.RecordObject(mgr, "Disable All Postures");

            mgr.EnableAllPostures(false);

            serializedObject.Update();

            mgr.EvaluateAllScores();

            EditorUtility.SetDirty(mgr);

        }

        EditorGUILayout.EndHorizontal();



        EditorGUILayout.Space(6);



        // Header tabel

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Aktif?", EditorStyles.miniBoldLabel, GUILayout.Width(45));

        EditorGUILayout.LabelField("Warna", EditorStyles.miniBoldLabel, GUILayout.Width(45));

        EditorGUILayout.LabelField("Label Postur", EditorStyles.miniBoldLabel, GUILayout.Width(110));

        EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel, GUILayout.Width(75));

        EditorGUILayout.LabelField("Akurasi (%)", EditorStyles.miniBoldLabel);

        EditorGUILayout.EndHorizontal();



        // Baris untuk setiap postur

        for (int i = 0; i < mgr.allPostureScores.Count; i++)

        {

            var item = mgr.allPostureScores[i];

            bool isEnabled = mgr.IsPostureEnabled(item.postureType);

            float normVal = Mathf.Clamp01(item.accuracyPercent / 100f);

            Color postureColor = mgr.GetPostureColor(item.postureType);



            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);



            // 1. Checkbox Toggle Enable/Disable Label

            bool newEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(35));

            if (newEnabled != isEnabled)

            {

                Undo.RecordObject(mgr, $"Toggle {item.postureType} Enabled");

                mgr.SetPostureEnabled(item.postureType, newEnabled);

                serializedObject.Update();

                mgr.EvaluateAllScores();

                EditorUtility.SetDirty(mgr);

            }



            // 2. Kotak sampel warna panel UI (redup jika nonaktif)

            Rect colorRect = EditorGUILayout.GetControlRect(false, 18, GUILayout.Width(45));

            Color boxColor = isEnabled ? new Color(postureColor.r, postureColor.g, postureColor.b, 1f) : new Color(0.35f, 0.35f, 0.35f, 0.5f);

            EditorGUI.DrawRect(colorRect, boxColor);



            // 3. Nama Label

            string labelText;

            if (!isEnabled)

            {

                labelText = $"<color=#777777>{item.labelIndonesian} <i>(Off)</i></color>";

            }

            else if (item.isDetectedWinner)

            {

                labelText = $"<b><color=#00FF88>★ {item.labelIndonesian}</color></b>";

            }

            else

            {

                labelText = item.labelIndonesian;

            }

            EditorGUILayout.LabelField(labelText, new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(110));



            // 4. Status Boolean Checkbox (Centang hijau jika terdeteksi & aktif)

            GUI.enabled = false;

            EditorGUILayout.Toggle(isEnabled && item.isDetectedWinner, GUILayout.Width(35));

            GUI.enabled = true;

            string boolStatus = (!isEnabled) 

                ? "<color=#555555>OFF</color>" 

                : (item.isDetectedWinner ? "<b><color=#2ECC71>TRUE</color></b>" : "<color=#888888>FALSE</color>");

            EditorGUILayout.LabelField(boolStatus, new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(40));



            // 5. Progress Bar Akurasi

            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(18));

            if (isEnabled)

            {

                EditorGUI.ProgressBar(r, normVal, $"{item.accuracyPercent:F1}%");

            }

            else

            {

                EditorGUI.ProgressBar(r, 0f, $"{item.accuracyPercent:F1}% (Nonaktif)");

            }



            EditorGUILayout.EndHorizontal();

        }

    }



    private void DrawShowcaseAndRecordSection(PoseCalibrationManager mgr)

    {

        EditorGUILayout.LabelField("3. Showcase Pose 100% & Tombol Rekam (Snapshot)", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(

            "• Otak-atik sendi / bola hijau dummy di Scene View sesuai keinginanmu.\n" +

            "• Klik 'Rekam' untuk menyimpan seluruh rotasi sendi & metriks ke postur tersebut (tombol akan berubah menjadi '✓ Terekam').\n" +

            "• Klik 'Showcase 100%' kapan saja untuk menampilkan kembali pose yang sudah kamu rekam!",

            MessageType.None);



        if (!string.IsNullOrEmpty(lastRecordedFeedback) && (EditorApplication.timeSinceStartup - lastRecordedTime) < 12.0)

        {

            GUI.backgroundColor = new Color(0.15f, 0.88f, 0.45f);

            EditorGUILayout.HelpBox(lastRecordedFeedback, MessageType.Info);

            GUI.backgroundColor = Color.white;

        }



        DrawPoseActionRow(mgr, PostureType.Standing, "Berdiri (Standing)", new Color(0.2f, 0.75f, 0.4f));

        DrawPoseActionRow(mgr, PostureType.Kneeling, "Berlutut (Kneeling)", new Color(0.25f, 0.65f, 0.9f));

        DrawPoseActionRow(mgr, PostureType.Squatting, "Jongkok (Squatting)", new Color(0.95f, 0.55f, 0.15f));

        DrawPoseActionRow(mgr, PostureType.SittingCrossLegged, "Duduk Sila (Cross-Legged)", new Color(0.6f, 0.4f, 0.95f));

        DrawPoseActionRow(mgr, PostureType.SittingEmok, "Duduk Emok (Bersimpuh)", new Color(0.95f, 0.35f, 0.65f));

        DrawPoseActionRow(mgr, PostureType.SittingChair, "Duduk Normal (SittingChair)", new Color(0.35f, 0.7f, 0.85f));



        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.85f, 0.85f, 0.95f);

        if (GUILayout.Button(">> RESET DUMMY KE T-POSE MURNI (BIND POSE FBX) <<", GUILayout.Height(30)))

        {

            if (mgr.referencePoseAvatar != null)

            {

                Undo.RegisterFullObjectHierarchyUndo(mgr.referencePoseAvatar, "Reset Dummy To Pure T-Pose");

            }

            Undo.RecordObject(mgr, "Reset Dummy To Pure T-Pose");

            mgr.ForceResetDummyToPureTPose();

            serializedObject.Update();

            MarkDummyDirtyAndRepaint(mgr);

        }

        GUI.backgroundColor = Color.white;

    }



    private void DrawLabelBoolToggle(PoseCalibrationManager mgr, PostureType type, string label)
    {
        bool cur = mgr.IsPostureEnabled(type);
        GUI.color = cur ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.7f);
        bool next = EditorGUILayout.ToggleLeft($"<b>{label}</b>", cur, GUILayout.Width(110));
        GUI.color = Color.white;
        if (next != cur)
        {
            Undo.RecordObject(mgr, $"Toggle {label} Enabled");
            mgr.SetPostureEnabled(type, next);
            serializedObject.Update();
            mgr.EvaluateAllScores();
            EditorUtility.SetDirty(mgr);
        }
    }

    private void DrawPoseActionRow(PoseCalibrationManager mgr, PostureType type, string label, Color color)
    {
        bool isRecorded = mgr.HasRecordedPose(type);
        bool isActive = mgr.IsShowcaseMode && mgr.ActiveShowcasePose == type;
        bool isEnabled = mgr.IsPostureEnabled(type);

        EditorGUILayout.BeginHorizontal();

        // 0. Checkbox Bool Langsung di Setiap Baris Tombol Pose
        bool nextEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));
        if (nextEnabled != isEnabled)
        {
            Undo.RecordObject(mgr, $"Toggle {type} Enabled");
            mgr.SetPostureEnabled(type, nextEnabled);
            serializedObject.Update();
            mgr.EvaluateAllScores();
            EditorUtility.SetDirty(mgr);
        }

        // 1. Tombol Showcase 100%
        Color baseCol = isEnabled ? color : Color.Lerp(color, Color.gray, 0.65f);
        GUI.backgroundColor = isActive ? Color.Lerp(baseCol, Color.white, 0.28f) : baseCol;
        string activePrefix = isActive ? "▶ " : "";
        string customTag = isRecorded ? " [Custom Terekam]" : "";
        string offTag = !isEnabled ? " (Off)" : "";
        string showcaseBtnLabel = $"{activePrefix}Showcase 100%: {label}{customTag}{offTag}";

        if (GUILayout.Button(showcaseBtnLabel, GUILayout.Height(28)))

        {

            if (mgr.referencePoseAvatar != null)

            {

                Undo.RegisterFullObjectHierarchyUndo(mgr.referencePoseAvatar, $"Showcase Pose {type}");

            }

            Undo.RecordObject(mgr, $"Showcase Pose {type}");

            mgr.ShowcasePose(type);

            serializedObject.Update();

            MarkDummyDirtyAndRepaint(mgr);

        }



        // 2. Tombol Rekam (berubah menjadi hijau "✓ Terekam" saat pose kustom sudah disimpan)

        GUI.backgroundColor = isRecorded ? new Color(0.18f, 0.82f, 0.42f) : new Color(0.25f, 0.35f, 0.45f);

        string recordBtnLabel = isRecorded ? "✓ Terekam" : "● Rekam";

        GUIContent recordContent = new GUIContent(

            recordBtnLabel,

            isRecorded

                ? "Pose kustom sudah tersimpan! Klik lagi jika ingin menimpa/mengupdate rekaman dengan posisi sendi saat ini."

                : "Klik untuk merekam posisi & rotasi seluruh sendi dummy saat ini sebagai pose 100%.");



        if (GUILayout.Button(recordContent, GUILayout.Width(90), GUILayout.Height(28)))

        {

            if (mgr.referencePoseAvatar != null)

            {

                Undo.RegisterFullObjectHierarchyUndo(mgr.referencePoseAvatar, $"Capture Pose {type}");

            }

            Undo.RecordObject(mgr, $"Capture Pose {type}");

            mgr.CaptureCurrentPoseAs(type);

            serializedObject.Update();

            lastRecordedFeedback = $"✓ Pose '{label}' berhasil direkam! (Pinggul: {mgr.LiveHipHeight:F2}m | Sudut Lutut: {mgr.LiveKneeAngle:F1}°)";

            lastRecordedTime = EditorApplication.timeSinceStartup;

            MarkDummyDirtyAndRepaint(mgr);

        }



        // 3. Tombol Reset Rekaman (↺) jika postur ini punya rekaman kustom

        if (isRecorded)

        {

            GUI.backgroundColor = new Color(0.85f, 0.35f, 0.35f);

            GUIContent resetContent = new GUIContent("↺", $"Hapus rekaman kustom '{label}' dan kembalikan ke pose bawaan");

            if (GUILayout.Button(resetContent, GUILayout.Width(28), GUILayout.Height(28)))

            {

                if (mgr.referencePoseAvatar != null)

                {

                    Undo.RegisterFullObjectHierarchyUndo(mgr.referencePoseAvatar, $"Reset Recorded Pose {type}");

                }

                Undo.RecordObject(mgr, $"Reset Recorded Pose {type}");

                mgr.ClearRecordedPose(type);

                serializedObject.Update();

                lastRecordedFeedback = $"↺ Rekaman kustom '{label}' di-reset kembali ke pose default.";

                lastRecordedTime = EditorApplication.timeSinceStartup;

                MarkDummyDirtyAndRepaint(mgr);

            }

        }



        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

    }



    private void MarkDummyDirtyAndRepaint(PoseCalibrationManager mgr)

    {

        EditorUtility.SetDirty(mgr);

        if (mgr.referencePoseAvatar != null)

        {

            EditorUtility.SetDirty(mgr.referencePoseAvatar);

            Transform[] children = mgr.referencePoseAvatar.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < children.Length; i++)

            {

                if (children[i] != null) EditorUtility.SetDirty(children[i]);

            }

        }

        SceneView.RepaintAll();

    }

}

#endif

