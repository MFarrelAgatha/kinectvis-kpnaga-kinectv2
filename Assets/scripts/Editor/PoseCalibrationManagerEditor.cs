#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PoseCalibrationManager))]
public class PoseCalibrationManagerEditor : Editor
{
    private bool showMetricsFold = true;
    private bool showThresholdsFold = false;

    public override void OnInspectorGUI()
    {
        PoseCalibrationManager mgr = (PoseCalibrationManager)target;

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
        EditorGUILayout.LabelField("2. Evaluasi Akurasi Seluruh Label & Status Bool Deteksi", EditorStyles.boldLabel);

        // Big Winner Box menggunakan warna panel yang sesuai
        Color winnerColor = mgr.GetPostureColor(mgr.DetectedPosture);
        GUI.backgroundColor = winnerColor;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUIStyle winnerStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        EditorGUILayout.LabelField($"TERDETEKSI: {mgr.DetectedPostureLabel.ToUpper()} ({mgr.TopAccuracyPercent:F1}%)", winnerStyle);
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(6);

        // Header tabel
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Warna UI", EditorStyles.miniBoldLabel, GUILayout.Width(60));
        EditorGUILayout.LabelField("Label Postur", EditorStyles.miniBoldLabel, GUILayout.Width(110));
        EditorGUILayout.LabelField("Terdeteksi?", EditorStyles.miniBoldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("Akurasi (%)", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();

        // Baris untuk setiap postur
        for (int i = 0; i < mgr.allPostureScores.Count; i++)
        {
            var item = mgr.allPostureScores[i];
            float normVal = Mathf.Clamp01(item.accuracyPercent / 100f);
            Color postureColor = mgr.GetPostureColor(item.postureType);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 1. Kotak sampel warna panel UI
            Rect colorRect = EditorGUILayout.GetControlRect(false, 18, GUILayout.Width(45));
            EditorGUI.DrawRect(colorRect, new Color(postureColor.r, postureColor.g, postureColor.b, 1f));

            // 2. Nama Label
            string labelText = item.isDetectedWinner ? $"<b><color=#00FF88>★ {item.labelIndonesian}</color></b>" : item.labelIndonesian;
            EditorGUILayout.LabelField(labelText, new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(110));

            // 3. Status Boolean Checkbox (Centang hijau jika terdeteksi)
            GUI.enabled = false;
            EditorGUILayout.Toggle(item.isDetectedWinner, GUILayout.Width(35));
            GUI.enabled = true;
            string boolStatus = item.isDetectedWinner ? "<b><color=#2ECC71>TRUE</color></b>" : "<color=#888888>FALSE</color>";
            EditorGUILayout.LabelField(boolStatus, new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(45));

            // 4. Progress Bar Akurasi
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(18));
            EditorGUI.ProgressBar(r, normVal, $"{item.accuracyPercent:F1}%");

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawShowcaseAndRecordSection(PoseCalibrationManager mgr)
    {
        EditorGUILayout.LabelField("3. Showcase Pose 100% & Tombol Rekam (Snapshot)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Klik tombol 'Showcase' untuk melihat langsung bentuk pose 100% pada dummy avatar, atau klik 'Rekam' untuk menyimpan pose dummy yang sedang kamu posisikan sendiri.", MessageType.None);

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
            MarkDummyDirtyAndRepaint(mgr);
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawPoseActionRow(PoseCalibrationManager mgr, PostureType type, string label, Color color)
    {
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = color;
        if (GUILayout.Button($"Showcase 100%: {label}", GUILayout.Height(26)))
        {
            if (mgr.referencePoseAvatar != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(mgr.referencePoseAvatar, $"Showcase Pose {type}");
            }
            Undo.RecordObject(mgr, $"Showcase Pose {type}");
            mgr.ShowcasePose(type);
            MarkDummyDirtyAndRepaint(mgr);
        }

        GUI.backgroundColor = new Color(0.25f, 0.35f, 0.45f);
        if (GUILayout.Button($"Rekam", GUILayout.Width(75), GUILayout.Height(26)))
        {
            Undo.RecordObject(mgr, $"Capture Pose {type}");
            mgr.CaptureCurrentPoseAs(type);
            EditorUtility.SetDirty(mgr);
            SceneView.RepaintAll();
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
