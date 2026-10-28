using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(KinectStatusWarningUI))]
public class KinectStatusWarningUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        KinectStatusWarningUI warningUI = (KinectStatusWarningUI)target;

        // Custom Header Banner
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        bool isDetected = warningUI.CheckIsKinectDetected();

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };

        if (isDetected)
        {
            GUI.color = new Color(0.3f, 1f, 0.4f);
            GUILayout.Label("STATUS: KINECT TERHUBUNG (STREAMING AKTIF)", headerStyle);
        }
        else
        {
            GUI.color = new Color(1f, 0.4f, 0.3f);
            GUILayout.Label("STATUS: KINECT BELUM TERDETEKSI (WARNING NYALA)", headerStyle);
        }
        GUI.color = Color.white;
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // Standard properties
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Testing & Kontrol:", EditorStyles.boldLabel);

        // Interactive Testing Buttons
        EditorGUILayout.BeginHorizontal();
        if (warningUI.simulateDisconnected)
        {
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            if (GUILayout.Button("Matikan Simulasi (Mode Normal)", GUILayout.Height(30)))
            {
                Undo.RecordObject(warningUI, "Toggle Simulate Disconnected");
                warningUI.simulateDisconnected = false;
                warningUI.RefreshStatus(true);
                EditorUtility.SetDirty(warningUI);
            }
        }
        else
        {
            GUI.backgroundColor = new Color(1f, 0.5f, 0.2f);
            if (GUILayout.Button("Test Simulasi Disconnect (Nyalakan Warning)", GUILayout.Height(30)))
            {
                Undo.RecordObject(warningUI, "Toggle Simulate Disconnected");
                warningUI.simulateDisconnected = true;
                warningUI.RefreshStatus(true);
                EditorUtility.SetDirty(warningUI);
            }
        }

        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("Rebuild UI di Canvas", GUILayout.Height(30)))
        {
            Undo.RecordObject(warningUI, "Rebuild Warning UI");
            warningUI.EnsureUIConstructed();
            warningUI.RefreshStatus(true);
            EditorUtility.SetDirty(warningUI);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
    }
}
