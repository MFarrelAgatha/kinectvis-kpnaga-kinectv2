using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Menampilkan Sphere di setiap joint dan Garis Penghubung (bones) antar joint karakter Kinect/Humanoid.
/// Dilengkapi bool toggle untuk enable/disable garis (between the lines) dan sphere sesuka hati di Inspector.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class JointSphereVisualizer : MonoBehaviour
{
    [Header("Garis Penghubung (Between The Lines)")]
    [Tooltip("Centang (true) untuk menampilkan garis antar sendi, uncheck (false) untuk menyembunyikan.")]
    public bool showConnectingLines = true;

    [Tooltip("Ketebalan garis tulang (dalam meter).")]
    [Range(0.005f, 0.08f)]
    public float lineWidth = 0.025f;

    [Tooltip("Warna garis penghubung antar sendi.")]
    public Color lineColor = new Color(0.15f, 1f, 0.35f, 0.9f); // Hijau terang

    [Header("Titik Sendi (Joint Spheres)")]
    [Tooltip("Centang (true) untuk menampilkan bola di setiap joint, uncheck (false) untuk menyembunyikan.")]
    public bool showJointSpheres = true;

    [Tooltip("Ukuran diameter bola pada setiap joint (dalam meter).")]
    [Range(0.02f, 0.3f)]
    public float sphereRadius = 0.07f;

    [Tooltip("Warna sphere joint.")]
    public Color jointColor = new Color(0.1f, 1f, 0.2f, 1f); // Hijau terang / lime

    [Header("Opsi Mesh Asli & Material")]
    [Tooltip("Gunakan shader unlit agar selalu terlihat terang tanpa bayangan gelap.")]
    public bool useUnlit = true;

    [Tooltip("Otomatis matikan semua SkinnedMeshRenderer asli di karakter ini.")]
    public bool disableOriginalMeshes = true;

    [Header("Auto Setup")]
    [Tooltip("Generate visualizer otomatis saat game dijalankan.")]
    public bool generateOnStart = true;

    [System.Serializable]
    private class BoneConnection
    {
        public Transform startBone;
        public Transform endBone;
        public LineRenderer lineRenderer;
    }

    [HideInInspector]
    [SerializeField]
    private List<GameObject> generatedSpheres = new List<GameObject>();

    [HideInInspector]
    [SerializeField]
    private List<BoneConnection> connections = new List<BoneConnection>();

    private Material sharedJointMaterial;
    private Material sharedLineMaterial;

    private bool lastShowLines = true;
    private bool lastShowSpheres = true;

    private void Start()
    {
        if (disableOriginalMeshes)
        {
            SetMeshesVisible(false);
        }

        if (generateOnStart)
        {
            if (generatedSpheres == null || generatedSpheres.Count == 0 || connections == null || connections.Count == 0)
            {
                GenerateVisualizer();
            }
        }
    }

    private void LateUpdate()
    {
        // 1. Tangani toggle Show/Hide Spheres
        if (lastShowSpheres != showJointSpheres)
        {
            lastShowSpheres = showJointSpheres;
            ApplySphereVisibility();
        }

        // 2. Tangani toggle Show/Hide Lines
        if (lastShowLines != showConnectingLines)
        {
            lastShowLines = showConnectingLines;
            ApplyLineVisibility();
        }

        // 3. Update posisi dan bentuk garis penghubung jika aktif
        if (showConnectingLines && connections != null && connections.Count > 0)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                var conn = connections[i];
                if (conn == null || conn.lineRenderer == null) continue;

                if (conn.startBone != null && conn.endBone != null && 
                    conn.startBone.gameObject.activeInHierarchy && conn.endBone.gameObject.activeInHierarchy)
                {
                    if (!conn.lineRenderer.enabled) conn.lineRenderer.enabled = true;

                    conn.lineRenderer.startWidth = lineWidth;
                    conn.lineRenderer.endWidth = lineWidth;
                    conn.lineRenderer.SetPosition(0, conn.startBone.position);
                    conn.lineRenderer.SetPosition(1, conn.endBone.position);
                }
                else
                {
                    if (conn.lineRenderer.enabled) conn.lineRenderer.enabled = false;
                }
            }
        }
    }

    private void ApplySphereVisibility()
    {
        if (generatedSpheres == null) return;
        for (int i = 0; i < generatedSpheres.Count; i++)
        {
            if (generatedSpheres[i] != null)
            {
                generatedSpheres[i].SetActive(showJointSpheres);
            }
        }
    }

    private void ApplyLineVisibility()
    {
        if (connections == null) return;
        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i] != null && connections[i].lineRenderer != null)
            {
                connections[i].lineRenderer.enabled = showConnectingLines;
            }
        }
    }

    private Material GetOrCreateJointMaterial()
    {
        if (sharedJointMaterial != null) return sharedJointMaterial;

        Shader shader = useUnlit ? (Shader.Find("Unlit/Color") ?? Shader.Find("Mobile/Unlit (Supports Lightmap)")) : null;
        if (shader == null) shader = Shader.Find("Standard");

        sharedJointMaterial = new Material(shader) { name = "VisJoint_Mat" };
        if (sharedJointMaterial.HasProperty("_Color")) sharedJointMaterial.SetColor("_Color", jointColor);
        if (sharedJointMaterial.HasProperty("_EmissionColor"))
        {
            sharedJointMaterial.EnableKeyword("_EMISSION");
            sharedJointMaterial.SetColor("_EmissionColor", jointColor * 0.8f);
        }
        return sharedJointMaterial;
    }

    private Material GetOrCreateLineMaterial()
    {
        if (sharedLineMaterial != null) return sharedLineMaterial;

        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        sharedLineMaterial = new Material(shader) { name = "VisLine_Mat" };
        if (sharedLineMaterial.HasProperty("_Color")) sharedLineMaterial.SetColor("_Color", lineColor);
        return sharedLineMaterial;
    }

    [ContextMenu("Generate Visualizer Now")]
    public void GenerateVisualizer()
    {
        ClearVisualizer();

        Material jointMat = GetOrCreateJointMaterial();
        Material lineMat = GetOrCreateLineMaterial();

        List<Transform> targetBones = CollectJointBones();
        if (targetBones.Count == 0)
        {
            Debug.LogWarning("[JointSphereVisualizer] Tidak ditemukan bone/joint pada " + gameObject.name);
            return;
        }

        // 1. Buat Spheres di tiap joint
        foreach (Transform bone in targetBones)
        {
            if (bone == null) continue;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "VisJoint_" + bone.name;

            Collider col = sphere.GetComponent<Collider>();
            if (col != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(col);
                else Destroy(col);
#else
                Destroy(col);
#endif
            }

            sphere.transform.SetParent(bone, false);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = Vector3.one * sphereRadius;

            Renderer rend = sphere.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = jointMat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }

            sphere.SetActive(showJointSpheres);
            generatedSpheres.Add(sphere);
        }

        // 2. Buat Lines penghubung antar joint
        GameObject linesHolder = new GameObject("VisLines_Holder");
        linesHolder.transform.SetParent(transform, false);

        List<KeyValuePair<Transform, Transform>> bonePairs = BuildBoneConnections(targetBones);
        foreach (var pair in bonePairs)
        {
            if (pair.Key == null || pair.Value == null) continue;

            GameObject lineObj = new GameObject($"Line_{pair.Key.name}_{pair.Value.name}");
            lineObj.transform.SetParent(linesHolder.transform, false);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.sharedMaterial = lineMat;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = showConnectingLines;

            lr.SetPosition(0, pair.Key.position);
            lr.SetPosition(1, pair.Value.position);

            connections.Add(new BoneConnection
            {
                startBone = pair.Key,
                endBone = pair.Value,
                lineRenderer = lr
            });
        }

        if (disableOriginalMeshes)
        {
            SetMeshesVisible(false);
        }

        lastShowLines = showConnectingLines;
        lastShowSpheres = showJointSpheres;

        Debug.Log($"[JointSphereVisualizer] Berhasil memasang {generatedSpheres.Count} sphere dan {connections.Count} garis penghubung.");
    }

    [ContextMenu("Clear Visualizer")]
    public void ClearVisualizer()
    {
        // Bersihkan spheres
        for (int i = generatedSpheres.Count - 1; i >= 0; i--)
        {
            if (generatedSpheres[i] != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(generatedSpheres[i]);
                else Destroy(generatedSpheres[i]);
#else
                Destroy(generatedSpheres[i]);
#endif
            }
        }
        generatedSpheres.Clear();

        // Bersihkan connections
        for (int i = connections.Count - 1; i >= 0; i--)
        {
            if (connections[i] != null && connections[i].lineRenderer != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(connections[i].lineRenderer.gameObject);
                else Destroy(connections[i].lineRenderer.gameObject);
#else
                Destroy(connections[i].lineRenderer.gameObject);
#endif
            }
        }
        connections.Clear();

        // Bersihkan sisa objek
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child != null && (child.name.StartsWith("VisJoint_") || child.name == "VisLines_Holder" || child.name.StartsWith("Line_")))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(child.gameObject);
                else Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }
    }

    [ContextMenu("Toggle Meshes (Show/Hide)")]
    public void ToggleOriginalMeshes()
    {
        SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (smrs.Length > 0)
        {
            bool newState = !smrs[0].enabled;
            SetMeshesVisible(newState);
            Debug.Log($"[JointSphereVisualizer] SkinnedMeshRenderer set to: {newState}");
        }
    }

    public void SetMeshesVisible(bool visible)
    {
        SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in smrs)
        {
            smr.enabled = visible;
        }

        MeshRenderer[] mrs = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in mrs)
        {
            if (mr.gameObject.name.StartsWith("VisJoint_") || mr.gameObject.name.StartsWith("Line_")) continue;
            mr.enabled = visible;
        }
    }

    private List<Transform> CollectJointBones()
    {
        HashSet<Transform> boneSet = new HashSet<Transform>();

        // 1. Coba dari Mecanim Animator
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            System.Array values = System.Enum.GetValues(typeof(HumanBodyBones));
            foreach (HumanBodyBones boneEnum in values)
            {
                if (boneEnum == HumanBodyBones.LastBone) continue;
                Transform t = anim.GetBoneTransform(boneEnum);
                if (t != null) boneSet.Add(t);
            }
        }

        // 2. Coba dari AvatarController
        AvatarController ac = GetComponent<AvatarController>() ?? GetComponentInChildren<AvatarController>();
        if (ac != null)
        {
            for (int i = 0; i < 30; i++)
            {
                Transform t = ac.GetBoneTransform(i);
                if (t != null) boneSet.Add(t);
            }
        }

        // 3. Fallback pencarian FBX joint
        if (boneSet.Count == 0)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allChildren)
            {
                if (t == transform) continue;
                if (t.name.StartsWith("VisJoint_") || t.name.StartsWith("Line_") || t.name == "VisLines_Holder") continue;

                string lower = t.name.ToLower();
                if (lower.Contains("joint") || lower.Contains("bone") || lower.Contains("pelvis") || 
                    lower.Contains("spine") || lower.Contains("hand") || lower.Contains("foot") || 
                    lower.Contains("arm") || lower.Contains("leg") || lower.Contains("head"))
                {
                    boneSet.Add(t);
                }
            }
        }

        return new List<Transform>(boneSet);
    }

    private List<KeyValuePair<Transform, Transform>> BuildBoneConnections(List<Transform> targetBones)
    {
        var pairs = new List<KeyValuePair<Transform, Transform>>();
        HashSet<string> addedPairs = new HashSet<string>();

        void TryAddPair(Transform a, Transform b)
        {
            if (a == null || b == null || a == b) return;
            string key = a.GetInstanceID() < b.GetInstanceID() 
                ? $"{a.GetInstanceID()}_{b.GetInstanceID()}" 
                : $"{b.GetInstanceID()}_{a.GetInstanceID()}";

            if (!addedPairs.Contains(key))
            {
                addedPairs.Add(key);
                pairs.Add(new KeyValuePair<Transform, Transform>(a, b));
            }
        }

        // A. Coba hubungkan via AvatarController bones
        AvatarController ac = GetComponent<AvatarController>() ?? GetComponentInChildren<AvatarController>();
        if (ac != null)
        {
            // Standard bone index pairs Kinect AvatarController
            int[,] boneLinks = new int[,]
            {
                {0, 1},   // Pelvis -> Spine
                {1, 2},   // Spine -> Neck/SpineShoulder
                {2, 3},   // Neck -> Head
                {2, 4},   // Neck -> LeftShoulder
                {4, 5},   // LeftShoulder -> LeftUpperArm
                {5, 6},   // LeftUpperArm -> LeftLowerArm
                {6, 7},   // LeftLowerArm -> LeftHand
                {2, 8},   // Neck -> RightShoulder
                {8, 9},   // RightShoulder -> RightUpperArm
                {9, 10},  // RightUpperArm -> RightLowerArm
                {10, 11}, // RightLowerArm -> RightHand
                {0, 12},  // Pelvis -> LeftUpperLeg
                {12, 13}, // LeftUpperLeg -> LeftLowerLeg
                {13, 14}, // LeftLowerLeg -> LeftFoot
                {14, 15}, // LeftFoot -> LeftToes
                {0, 16},  // Pelvis -> RightUpperLeg
                {16, 17}, // RightUpperLeg -> RightLowerLeg
                {17, 18}, // RightLowerLeg -> RightFoot
                {18, 19}  // RightFoot -> RightToes
            };

            for (int i = 0; i < boneLinks.GetLength(0); i++)
            {
                Transform b1 = ac.GetBoneTransform(boneLinks[i, 0]);
                Transform b2 = ac.GetBoneTransform(boneLinks[i, 1]);
                TryAddPair(b1, b2);
            }
        }

        // B. Coba via Mecanim Animator
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman && pairs.Count == 0)
        {
            HumanBodyBones[,] mecanimLinks = new HumanBodyBones[,]
            {
                {HumanBodyBones.Hips, HumanBodyBones.Spine},
                {HumanBodyBones.Spine, HumanBodyBones.Chest},
                {HumanBodyBones.Chest, HumanBodyBones.Neck},
                {HumanBodyBones.Neck, HumanBodyBones.Head},
                {HumanBodyBones.Chest, HumanBodyBones.LeftShoulder},
                {HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm},
                {HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm},
                {HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand},
                {HumanBodyBones.Chest, HumanBodyBones.RightShoulder},
                {HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm},
                {HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm},
                {HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand},
                {HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg},
                {HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg},
                {HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot},
                {HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes},
                {HumanBodyBones.Hips, HumanBodyBones.RightUpperLeg},
                {HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg},
                {HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot},
                {HumanBodyBones.RightFoot, HumanBodyBones.RightToes}
            };

            for (int i = 0; i < mecanimLinks.GetLength(0); i++)
            {
                Transform b1 = anim.GetBoneTransform(mecanimLinks[i, 0]);
                Transform b2 = anim.GetBoneTransform(mecanimLinks[i, 1]);
                // Fallback jika tidak ada Chest, hubungkan Spine langsung ke Neck / Shoulder
                if (b1 == null && mecanimLinks[i, 0] == HumanBodyBones.Chest)
                    b1 = anim.GetBoneTransform(HumanBodyBones.Spine);
                TryAddPair(b1, b2);
            }
        }

        // C. Fallback: Hubungkan setiap joint ke parent joint-nya dalam hierarki
        if (pairs.Count == 0)
        {
            HashSet<Transform> set = new HashSet<Transform>(targetBones);
            foreach (Transform bone in targetBones)
            {
                Transform p = bone.parent;
                while (p != null && p != transform)
                {
                    if (set.Contains(p))
                    {
                        TryAddPair(p, bone);
                        break;
                    }
                    p = p.parent;
                }
            }
        }

        return pairs;
    }
}
