using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// KinectFrustumVisualizer
/// Visualizes the exact Xbox Kinect sensor detection frustum (FOV cone, clipping planes,
/// floor footprint, and distance radius rings) in Unity Scene view and Game view.
/// Calibrated for 1:1 real-world house replica alignment and spatial ergonomics.
/// Pulls origin, orientation, and intrinsics directly from the Xbox Kinect sensor (KinectManager).
/// </summary>
[ExecuteAlways]
public class KinectFrustumVisualizer : MonoBehaviour
{

    public enum SensorMode
    {
        DepthSensor_BodyTracking, // Kinect v2 Depth/IR camera (used for body tracking): 70.6° H x 60° V
        ColorCamera               // Kinect v2 RGB camera: 84.1° H x 53.8° V
    }

    [Header("Master Debug Toggles")]
    [Tooltip("Master switch: Turn all visual debugging ON or OFF completely.")]
    public bool enableVisualizer = true;

    [Tooltip("Toggle all In-Game debug visuals (floor footprint lines, radius rings, gradient walls) in the Game view.")]
    public bool showInGameDebug = true;

    [Tooltip("Toggle all Gizmos and text overlays in the Scene view.")]
    public bool showSceneViewDebug = true;

    [Header("Sensor Camera Placement")]
    [Tooltip("Drag the Camera representing the real-life Kinect sensor in the scene here. The frustum, floor lines, and radius rings will project directly from this camera.")]
    public Camera sensorCamera;

    [Tooltip("Optional: If checked, ignores the Camera and places the frustum at KinectManager's raw coordinate origin (0, sensorHeight, 0).")]
    public bool useKinectManagerOrigin = false;

    [Header("Sensor Mode & SDK Sync")]
    [Tooltip("Select which sensor camera frustum to visualize. Depth Sensor is used for skeleton & body tracking.")]
    public SensorMode sensorMode = SensorMode.DepthSensor_BodyTracking;

    [Tooltip("Automatically query camera parameters from KinectManager / KinectInterop when available.")]
    public bool autoSyncWithKinectSDK = true;

    [Header("Frustum Intrinsics (Exact Kinect v2 Factory Specs)")]
    [Range(20f, 130f)]
    [Tooltip("Horizontal Field of View (degrees). Kinect v2 Depth: ~70.6°, Color: ~84.1°")]
    public float horizontalFOV = 70.6f;

    [Range(20f, 120f)]
    [Tooltip("Vertical Field of View (degrees). Kinect v2 Depth: 60.0°, Color: 53.8°")]
    public float verticalFOV = 60.0f;

    [Range(0.1f, 2.0f)]
    [Tooltip("Near clipping plane (meters). Kinect v2 minimum reliable detection is 0.5m. Closer than this is blind.")]
    public float nearClip = 0.5f;

    [Range(1.0f, 10.0f)]
    [Tooltip("Far clipping plane (meters). Kinect v2 standard body tracking limit is ~4.5m (raw depth up to 8.0m).")]
    public float farClip = 4.5f;

    [Header("Floor Projection & Radius Rings (Game View & Scene)")]
    [Tooltip("Render floor projection and radius rings in the Game view using LineRenderers and Meshes.")]
    public bool showFloorInGameView = true;

    [Tooltip("Drag your floor plane or floor GameObject here. If empty, auto-detects floor via downward Raycast or world Y=0.")]
    public Transform floorTransform;

    [Tooltip("Draw distance radius rings/arcs on the floor (e.g. 1m, 2m, 3m, 4m from sensor).")]
    public bool showRadiusRings = true;

    [Range(0.5f, 2.0f)]
    [Tooltip("Distance step interval between radius rings in meters (Default: 1.0m).")]
    public float radiusRingInterval = 1.0f;

    [Tooltip("Draw the exact floor detection footprint (sector boundaries where Kinect FOV covers the floor).")]
    public bool showFloorFootprint = true;

    [Range(0.005f, 0.1f)]
    [Tooltip("Line thickness of the floor lines in Game view.")]
    public float floorLineWidth = 0.03f;

    [Header("Vertical Gradient Wall (Hologram Curtain)")]
    [Tooltip("Extrude the floor boundary lines upwards into a vertical hologram wall barrier with a gradient that fades to transparent at the top.")]
    public bool enableVerticalGradientWall = true;

    [Range(0.1f, 3.0f)]
    [Tooltip("Height of the vertical gradient wall rising from the floor (meters).")]
    public float wallHeight = 0.7f;

    [Range(0.05f, 1.0f)]
    [Tooltip("Opacity at the base of the wall (near the floor). The top fades smoothly to 0 (fully transparent).")]
    public float wallBaseAlpha = 0.5f;

    [Tooltip("Also generate vertical gradient walls for each radius distance ring.")]
    public bool includeRadiusRingWalls = false;

    [Header("Floor Line Colors")]
    [Tooltip("Color of the floor footprint boundary lines.")]
    public Color floorFootprintColor = new Color(0.0f, 0.85f, 1.0f, 0.95f);

    [Tooltip("Color of the radius distance rings on the floor.")]
    public Color radiusRingColor = new Color(1.0f, 0.82f, 0.1f, 0.9f);

    [Tooltip("Color of the near clip blind boundary arc on the floor.")]
    public Color nearBlindFloorColor = new Color(1.0f, 0.25f, 0.2f, 0.9f);

    [Header("Optimal Tracking Zone (Sweet Spot)")]
    [Tooltip("Draw intermediate lines highlighting the optimal body tracking range (recommended 1.2m to 3.2m).")]
    public bool showSweetSpotZone = true;

    [Range(0.5f, 3.0f)]
    [Tooltip("Start of the optimal body tracking range (meters).")]
    public float sweetSpotNear = 1.2f;

    [Range(1.5f, 6.0f)]
    [Tooltip("End of the optimal body tracking range (meters).")]
    public float sweetSpotFar = 3.2f;

    [Header("Visual Styling & 3D Frustum")]
    [Tooltip("Draw frustum gizmos only when this GameObject is selected in Hierarchy, or always draw in Scene view.")]
    public bool drawOnlyWhenSelected = false;

    [Tooltip("Also render the full 3D frustum wireframe pyramid in Game view (not just floor lines).")]
    public bool show3DFrustumInGame = false;

    [Tooltip("Color of the main frustum wireframe edges and far plane.")]
    public Color frustumColor = new Color(0.0f, 0.75f, 1.0f, 0.9f);

    [Tooltip("Color of the near clip plane (blind zone barrier).")]
    public Color nearClipColor = new Color(1.0f, 0.25f, 0.2f, 0.85f);

    [Tooltip("Color of the optimal tracking sweet spot boundary.")]
    public Color sweetSpotColor = new Color(0.2f, 1.0f, 0.4f, 0.85f);

    [Tooltip("Draw center optical sightline axis from sensor origin.")]
    public bool showCenterLine = true;

    [Tooltip("Draw vertical plumb line from sensor to floor for real-life mounting height verification.")]
    public bool showFloorPlumbLine = true;

    [Tooltip("Fill the 3D frustum volume with a semi-transparent mesh in Scene view.")]
    public bool fillVolume = true;

    [Range(0.01f, 0.4f)]
    [Tooltip("Opacity of the volume fill.")]
    public float volumeFillAlpha = 0.08f;

    [Header("Scene View Labels")]
    [Tooltip("Draw informative text overlays and radius numbers in Scene view.")]
    public bool showSceneLabels = true;

    // Internal state cache
    private string lastSdkStatus = "Manual Calibration";
    private GameObject runtimeVisualsContainer;
    private LineRenderer frustum3DLineRenderer;
    private LineRenderer floorSectorLineRenderer;
    private List<LineRenderer> radiusRingRenderers = new List<LineRenderer>();
    private MeshFilter floorCurtainMeshFilter;
    private Mesh floorCurtainMesh;
    private List<MeshFilter> ringCurtainMeshFilters = new List<MeshFilter>();
    private List<Mesh> ringCurtainMeshes = new List<Mesh>();
    private Material lineMaterial;

    /// <summary>
    /// Computes the exact 3D position and orientation vectors for the sensor.
    /// By default, anchors directly to sensorCamera (or the GameObject), projecting the Xbox Kinect detection cone from that position.
    /// If useKinectManagerOrigin is enabled, projects from KinectManager's raw coordinate origin.
    /// </summary>
    public void GetSensorOriginAndOrientation(out Vector3 origin, out Vector3 fwd, out Vector3 up, out Vector3 right, out string originName, out float pitchAngle)
    {
        if (useKinectManagerOrigin)
        {
            KinectManager km = KinectManager.Instance != null ? KinectManager.Instance : FindAnyObjectByType<KinectManager>();
            if (km != null)
            {
                // KinectManager places sensor at local (0, sensorHeight, 0) rotated by -sensorAngle on X
                Vector3 localSensorPos = new Vector3(0f, km.sensorHeight, 0f);
                Quaternion localSensorRot = Quaternion.Euler(-km.sensorAngle, 0f, 0f);

                origin = km.transform.TransformPoint(localSensorPos);
                Quaternion worldRot = km.transform.rotation * localSensorRot;

                fwd = worldRot * Vector3.forward;
                up = worldRot * Vector3.up;
                right = worldRot * Vector3.right;
                originName = $"Xbox Kinect (via {km.gameObject.name})";
                pitchAngle = km.sensorAngle;
                return;
            }
        }

        Transform targetTransform = sensorCamera != null ? sensorCamera.transform : transform;
        origin = targetTransform.position;
        fwd = targetTransform.forward;
        up = targetTransform.up;
        right = targetTransform.right;
        originName = sensorCamera != null ? $"Camera: {sensorCamera.name}" : $"GameObject: {gameObject.name}";
        pitchAngle = -targetTransform.eulerAngles.x;
    }

    void Awake()
    {
        AutoFindReferences();
    }

    void OnValidate()
    {
        nearClip = Mathf.Max(0.05f, nearClip);
        farClip = Mathf.Max(nearClip + 0.1f, farClip);
        sweetSpotNear = Mathf.Clamp(sweetSpotNear, nearClip, farClip);
        sweetSpotFar = Mathf.Clamp(sweetSpotFar, sweetSpotNear, farClip);
        radiusRingInterval = Mathf.Max(0.25f, radiusRingInterval);
        wallHeight = Mathf.Max(0.05f, wallHeight);

        AutoFindReferences();
    }

    private void AutoFindReferences()
    {
        if (sensorCamera == null)
        {
            // 1. If script is attached directly to Camera
            sensorCamera = GetComponent<Camera>();

            // 2. Try to get sensorCamera from PlayerTungkuTracker
            if (sensorCamera == null)
            {
                PlayerTungkuTracker tracker = FindAnyObjectByType<PlayerTungkuTracker>();
                if (tracker != null && tracker.sensorCamera != null)
                {
                    sensorCamera = tracker.sensorCamera;
                }
            }

            // 3. Fallback to Camera.main
            if (sensorCamera == null)
            {
                sensorCamera = Camera.main;
            }
        }

        if (floorTransform == null)
        {
            KinectSessionManager sessionMgr = FindAnyObjectByType<KinectSessionManager>();
            if (sessionMgr != null && sessionMgr.floorTransform != null)
            {
                floorTransform = sessionMgr.floorTransform;
            }
        }
    }

    void Update()
    {
        if (!enableVisualizer)
        {
            CleanupRuntimeVisuals();
            return;
        }

        if (autoSyncWithKinectSDK)
        {
            TrySyncWithSDK();
        }

        if (showInGameDebug)
        {
            UpdateRuntimeGameViewLines();
        }
        else
        {
            CleanupRuntimeVisuals();
        }
    }

    void OnDisable()
    {
        CleanupRuntimeVisuals();
    }

    void OnDestroy()
    {
        CleanupRuntimeVisuals();
    }

    /// <summary>
    /// Queries sensor data directly from the active Xbox Kinect SDK (KinectManager & KinectInterop).
    /// </summary>
    public void TrySyncWithSDK()
    {
        KinectManager km = KinectManager.Instance != null ? KinectManager.Instance : FindAnyObjectByType<KinectManager>();

        if (km != null && km.IsInitialized())
        {
            KinectInterop.SensorData sensorData = km.GetSensorData();
            if (sensorData != null)
            {
                lastSdkStatus = "Synced with Xbox Kinect SDK";

                if (sensorMode == SensorMode.DepthSensor_BodyTracking)
                {
                    if (sensorData.depthCameraFOV > 0f)
                    {
                        verticalFOV = sensorData.depthCameraFOV; // 60.0°
                    }

                    if (sensorData.depthImageWidth > 0 && sensorData.depthImageHeight > 0)
                    {
                        float aspect = (float)sensorData.depthImageWidth / sensorData.depthImageHeight;
                        horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(verticalFOV * 0.5f * Mathf.Deg2Rad) * aspect) * Mathf.Rad2Deg; // 70.6°
                    }
                    else
                    {
                        horizontalFOV = 70.6f;
                    }
                }
                else // ColorCamera
                {
                    if (sensorData.colorCameraFOV > 0f)
                    {
                        verticalFOV = sensorData.colorCameraFOV; // 53.8°
                    }

                    if (sensorData.colorImageWidth > 0 && sensorData.colorImageHeight > 0)
                    {
                        float aspect = (float)sensorData.colorImageWidth / sensorData.colorImageHeight;
                        horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(verticalFOV * 0.5f * Mathf.Deg2Rad) * aspect) * Mathf.Rad2Deg; // 84.1°
                    }
                    else
                    {
                        horizontalFOV = 84.1f;
                    }
                }

                if (km.minUserDistance > 0f)
                {
                    nearClip = km.minUserDistance;
                }

                if (km.maxUserDistance > nearClip)
                {
                    farClip = km.maxUserDistance;
                }

                return;
            }
        }

        lastSdkStatus = "Calibrated to Xbox Kinect v2 Specs";
    }

    /// <summary>
    /// Calculates the effective floor Y position in world space.
    /// </summary>
    public float GetFloorY(Vector3 sensorOrigin)
    {
        if (floorTransform != null)
        {
            return floorTransform.position.y + 0.015f;
        }

        RaycastHit hit;
        if (Physics.Raycast(sensorOrigin, Vector3.down, out hit, 15f))
        {
            return hit.point.y + 0.015f;
        }

        return 0.015f;
    }

    [ContextMenu("Reset to Xbox Kinect v2 Depth Factory Specs")]
    public void ResetToKinectV2DepthSpecs()
    {
        sensorMode = SensorMode.DepthSensor_BodyTracking;
        horizontalFOV = 70.6f;
        verticalFOV = 60.0f;
        nearClip = 0.5f;
        farClip = 4.5f;
        sweetSpotNear = 1.2f;
        sweetSpotFar = 3.2f;
    }

    // ----------------------------------------------------
    // GIZMOS DRAWING (Scene View)
    // ----------------------------------------------------
    void OnDrawGizmos()
    {
        if (!enableVisualizer || !showSceneViewDebug) return;

        if (!drawOnlyWhenSelected)
        {
            DrawAllVisualsGizmo();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!enableVisualizer || !showSceneViewDebug) return;

        if (drawOnlyWhenSelected)
        {
            DrawAllVisualsGizmo();
        }
    }

    private void DrawAllVisualsGizmo()
    {
        Vector3 origin, fwd, up, right;
        string originName;
        float pitchAngle;
        GetSensorOriginAndOrientation(out origin, out fwd, out up, out right, out originName, out pitchAngle);

        float floorY = GetFloorY(origin);

        float halfVRad = (verticalFOV * 0.5f) * Mathf.Deg2Rad;
        float halfHRad = (horizontalFOV * 0.5f) * Mathf.Deg2Rad;
        float tanV = Mathf.Tan(halfVRad);
        float tanH = Mathf.Tan(halfHRad);

        Vector3[] nearCorners = GetFrustumCornersAtDistance(origin, fwd, up, right, nearClip, tanV, tanH);
        Vector3[] farCorners = GetFrustumCornersAtDistance(origin, fwd, up, right, farClip, tanV, tanH);

        // 1. Draw 3D Frustum Wireframe
        Gizmos.color = frustumColor;
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(origin, nearCorners[i]);
            Gizmos.DrawLine(nearCorners[i], farCorners[i]);
        }

        // Near plane (Blind boundary)
        Gizmos.color = nearClipColor;
        DrawQuadLines(nearCorners);

        // Far plane
        Gizmos.color = frustumColor;
        DrawQuadLines(farCorners);

        // 2. Sweet spot zone in 3D
        if (showSweetSpotZone && sweetSpotNear > nearClip && sweetSpotFar < farClip)
        {
            Gizmos.color = sweetSpotColor;
            Vector3[] sweetNearCorners = GetFrustumCornersAtDistance(origin, fwd, up, right, sweetSpotNear, tanV, tanH);
            Vector3[] sweetFarCorners = GetFrustumCornersAtDistance(origin, fwd, up, right, sweetSpotFar, tanV, tanH);
            DrawQuadLines(sweetNearCorners);
            DrawQuadLines(sweetFarCorners);
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(sweetNearCorners[i], sweetFarCorners[i]);
            }
        }

        // 3. Center Sightline
        if (showCenterLine)
        {
            Gizmos.color = new Color(frustumColor.r, frustumColor.g, frustumColor.b, 0.45f);
            Gizmos.DrawLine(origin, origin + fwd * farClip);
        }

        // 4. Plumb line to Floor
        if (showFloorPlumbLine)
        {
            Gizmos.color = Color.yellow;
            Vector3 groundPoint = new Vector3(origin.x, floorY, origin.z);
            Gizmos.DrawLine(origin, groundPoint);
            Gizmos.DrawWireSphere(groundPoint, 0.08f);
        }

        // 5. Sensor Origin Marker
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(origin, 0.06f);

        // 6. Volumetric Mesh Fill
        if (fillVolume)
        {
            DrawFrustumVolumeMesh(nearCorners, farCorners);
        }

        // 7. Floor Footprint & Radius Rings in Scene Gizmos
        DrawFloorVisualsGizmo(origin, fwd, floorY);

#if UNITY_EDITOR
        // 8. Scene View Labels
        if (showSceneLabels && !Application.isPlaying)
        {
            DrawSceneViewLabels(origin, fwd, up, right, nearCorners, farCorners, floorY, originName, pitchAngle);
        }
#endif
    }

    private void DrawFloorVisualsGizmo(Vector3 origin, Vector3 fwd, float floorY)
    {
        Vector3 floorOrigin = new Vector3(origin.x, floorY, origin.z);
        Vector3 fwdXZ = new Vector3(fwd.x, 0f, fwd.z).normalized;
        if (fwdXZ.sqrMagnitude < 0.001f) fwdXZ = Vector3.forward;

        float halfHAngle = (horizontalFOV * 0.5f);

        // A. Floor Footprint Sector
        if (showFloorFootprint)
        {
            // Far arc
            Gizmos.color = floorFootprintColor;
            DrawArcGizmo(floorOrigin, fwdXZ, farClip, -halfHAngle, halfHAngle, 32);

            // Near arc (Blind boundary)
            Gizmos.color = nearBlindFloorColor;
            DrawArcGizmo(floorOrigin, fwdXZ, nearClip, -halfHAngle, halfHAngle, 20);

            // Boundary rays (Left & Right edges connecting near to far)
            Gizmos.color = floorFootprintColor;
            Vector3 leftNear = floorOrigin + Quaternion.Euler(0f, -halfHAngle, 0f) * fwdXZ * nearClip;
            Vector3 leftFar = floorOrigin + Quaternion.Euler(0f, -halfHAngle, 0f) * fwdXZ * farClip;
            Vector3 rightNear = floorOrigin + Quaternion.Euler(0f, halfHAngle, 0f) * fwdXZ * nearClip;
            Vector3 rightFar = floorOrigin + Quaternion.Euler(0f, halfHAngle, 0f) * fwdXZ * farClip;

            Gizmos.DrawLine(leftNear, leftFar);
            Gizmos.DrawLine(rightNear, rightFar);

            // Scene View Vertical Gradient Ribbon
            if (enableVerticalGradientWall)
            {
                DrawCurtainGizmoSector(floorOrigin, fwdXZ, nearClip, farClip, halfHAngle, wallHeight, floorFootprintColor, wallBaseAlpha);
            }
        }

        // B. Concentric Radius Rings on the Floor
        if (showRadiusRings)
        {
            Gizmos.color = radiusRingColor;
            for (float r = radiusRingInterval; r < farClip; r += radiusRingInterval)
            {
                if (r <= nearClip + 0.1f) continue;
                DrawArcGizmo(floorOrigin, fwdXZ, r, -halfHAngle, halfHAngle, 28);
            }
        }
    }

    private void DrawArcGizmo(Vector3 center, Vector3 fwdXZ, float radius, float fromAngle, float toAngle, int segments)
    {
        float angleStep = (toAngle - fromAngle) / segments;
        Vector3 prevPoint = center + Quaternion.Euler(0f, fromAngle, 0f) * fwdXZ * radius;

        for (int i = 1; i <= segments; i++)
        {
            float currAngle = fromAngle + i * angleStep;
            Vector3 currPoint = center + Quaternion.Euler(0f, currAngle, 0f) * fwdXZ * radius;
            Gizmos.DrawLine(prevPoint, currPoint);
            prevPoint = currPoint;
        }
    }

    private void DrawCurtainGizmoSector(Vector3 center, Vector3 fwdXZ, float near, float far, float halfH, float height, Color color, float baseAlpha)
    {
#if UNITY_EDITOR
        Color baseCol = new Color(color.r, color.g, color.b, baseAlpha * 0.35f);
        int steps = 16;
        float stepAngle = (halfH * 2f) / steps;

        for (int i = 0; i < steps; i++)
        {
            float a0 = -halfH + i * stepAngle;
            float a1 = a0 + stepAngle;
            Vector3 p0 = center + Quaternion.Euler(0f, a0, 0f) * fwdXZ * far;
            Vector3 p1 = center + Quaternion.Euler(0f, a1, 0f) * fwdXZ * far;
            DrawQuadPoly(p0, p0 + Vector3.up * height, p1 + Vector3.up * height, p1, baseCol);
        }

        Vector3 leftNear = center + Quaternion.Euler(0f, -halfH, 0f) * fwdXZ * near;
        Vector3 leftFar = center + Quaternion.Euler(0f, -halfH, 0f) * fwdXZ * far;
        DrawQuadPoly(leftNear, leftNear + Vector3.up * height, leftFar + Vector3.up * height, leftFar, baseCol);

        Vector3 rightNear = center + Quaternion.Euler(0f, halfH, 0f) * fwdXZ * near;
        Vector3 rightFar = center + Quaternion.Euler(0f, halfH, 0f) * fwdXZ * far;
        DrawQuadPoly(rightNear, rightNear + Vector3.up * height, rightFar + Vector3.up * height, rightFar, baseCol);
#endif
    }

    private Vector3[] GetFrustumCornersAtDistance(Vector3 origin, Vector3 fwd, Vector3 up, Vector3 right, float dist, float tanV, float tanH)
    {
        float halfH = dist * tanH;
        float halfV = dist * tanV;
        Vector3 center = origin + fwd * dist;

        return new Vector3[]
        {
            center + up * halfV - right * halfH,
            center + up * halfV + right * halfH,
            center - up * halfV + right * halfH,
            center - up * halfV - right * halfH
        };
    }

    private void DrawQuadLines(Vector3[] corners)
    {
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }

    private void DrawFrustumVolumeMesh(Vector3[] near, Vector3[] far)
    {
        Color fillColor = new Color(frustumColor.r, frustumColor.g, frustumColor.b, volumeFillAlpha);

        DrawQuadPoly(near[0], far[0], far[1], near[1], fillColor);
        DrawQuadPoly(near[3], far[3], far[2], near[2], fillColor);
        DrawQuadPoly(near[0], far[0], far[3], near[3], fillColor);
        DrawQuadPoly(near[1], far[1], far[2], near[2], fillColor);

        DrawQuadPoly(near[0], near[1], near[2], near[3], new Color(nearClipColor.r, nearClipColor.g, nearClipColor.b, volumeFillAlpha * 1.5f));
        DrawQuadPoly(far[0], far[1], far[2], far[3], fillColor);
    }

    private void DrawQuadPoly(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = col;
        UnityEditor.Handles.DrawAAConvexPolygon(a, b, c, d);
#endif
    }

#if UNITY_EDITOR
    private void DrawSceneViewLabels(Vector3 origin, Vector3 fwd, Vector3 up, Vector3 right, Vector3[] near, Vector3[] far, float floorY, string originName, float pitchAngle)
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 11;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleLeft;

        string modeStr = (sensorMode == SensorMode.DepthSensor_BodyTracking) ? "Xbox Kinect Depth / Skeleton" : "Xbox Kinect Color RGB";
        string originInfo = $"[{originName}]\nMode: {modeStr}\nH-FOV: {horizontalFOV:F1}° | V-FOV: {verticalFOV:F1}°\nHeight: {origin.y:F2}m | Pitch: {pitchAngle:F1}°\nStatus: {lastSdkStatus}";
        Handles.Label(origin + up * 0.22f, originInfo, style);

        GUIStyle nearStyle = new GUIStyle(style);
        nearStyle.normal.textColor = new Color(1.0f, 0.5f, 0.4f);
        Handles.Label(near[0] + up * 0.08f, $"Near Blind Limit: {nearClip:F2}m", nearStyle);

        GUIStyle farStyle = new GUIStyle(style);
        farStyle.normal.textColor = new Color(0.4f, 0.9f, 1.0f);
        Handles.Label(far[0] + up * 0.12f, $"Far Detection Limit: {farClip:F2}m", farStyle);

        if (showRadiusRings)
        {
            GUIStyle ringLabelStyle = new GUIStyle(style);
            ringLabelStyle.normal.textColor = new Color(1.0f, 0.88f, 0.2f);
            ringLabelStyle.alignment = TextAnchor.MiddleCenter;

            Vector3 floorOrigin = new Vector3(origin.x, floorY, origin.z);
            Vector3 fwdXZ = new Vector3(fwd.x, 0f, fwd.z).normalized;
            if (fwdXZ.sqrMagnitude < 0.001f) fwdXZ = Vector3.forward;

            for (float r = radiusRingInterval; r < farClip; r += radiusRingInterval)
            {
                if (r <= nearClip + 0.1f) continue;
                Vector3 ringMid = floorOrigin + fwdXZ * r;
                Handles.Label(ringMid + Vector3.up * 0.04f, $"Radius {r:F1}m", ringLabelStyle);
            }
        }
    }
#endif

    // ----------------------------------------------------
    // RUNTIME LINE RENDERERS & MESH WALLS (Game View Display)
    // ----------------------------------------------------
    private Material GetOrCreateLineMaterial()
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
        }
        return lineMaterial;
    }

    private void EnsureVisualsContainer()
    {
        if (runtimeVisualsContainer == null)
        {
            Transform existing = transform.Find("[FrustumRuntimeVisuals]");
            if (existing != null)
            {
                runtimeVisualsContainer = existing.gameObject;
            }
            else
            {
                runtimeVisualsContainer = new GameObject("[FrustumRuntimeVisuals]");
                runtimeVisualsContainer.transform.SetParent(transform, false);
                runtimeVisualsContainer.hideFlags = HideFlags.DontSave;
            }
        }
    }

    private LineRenderer CreateOrGetSubLineRenderer(string childName, Color color, float width)
    {
        EnsureVisualsContainer();
        Transform childTr = runtimeVisualsContainer.transform.Find(childName);
        GameObject childObj;
        if (childTr == null)
        {
            childObj = new GameObject(childName);
            childObj.transform.SetParent(runtimeVisualsContainer.transform, false);
            childObj.hideFlags = HideFlags.DontSave;
        }
        else
        {
            childObj = childTr.gameObject;
        }

        LineRenderer lr = childObj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = childObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.material = GetOrCreateLineMaterial();
        }

        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = color;
        lr.endColor = color;
        lr.enabled = true;
        return lr;
    }

    private MeshFilter CreateOrGetSubMeshFilter(string childName, out MeshRenderer mr)
    {
        EnsureVisualsContainer();
        Transform childTr = runtimeVisualsContainer.transform.Find(childName);
        GameObject childObj;
        if (childTr == null)
        {
            childObj = new GameObject(childName);
            childObj.transform.SetParent(runtimeVisualsContainer.transform, false);
            childObj.hideFlags = HideFlags.DontSave;
        }
        else
        {
            childObj = childTr.gameObject;
        }

        MeshFilter mf = childObj.GetComponent<MeshFilter>();
        if (mf == null) mf = childObj.AddComponent<MeshFilter>();

        mr = childObj.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = childObj.AddComponent<MeshRenderer>();
            mr.material = GetOrCreateLineMaterial();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        childObj.SetActive(true);
        return mf;
    }

    private void UpdateRuntimeGameViewLines()
    {
        if (!showFloorInGameView && !show3DFrustumInGame)
        {
            CleanupRuntimeVisuals();
            return;
        }

        EnsureVisualsContainer();
        Vector3 origin, fwd, up, right;
        string originName;
        float pitchAngle;
        GetSensorOriginAndOrientation(out origin, out fwd, out up, out right, out originName, out pitchAngle);

        float floorY = GetFloorY(origin);

        float halfVRad = (verticalFOV * 0.5f) * Mathf.Deg2Rad;
        float halfHRad = (horizontalFOV * 0.5f) * Mathf.Deg2Rad;
        float tanV = Mathf.Tan(halfVRad);
        float tanH = Mathf.Tan(halfHRad);

        // 1. Optional 3D Frustum in Game View
        if (show3DFrustumInGame)
        {
            if (frustum3DLineRenderer == null)
            {
                frustum3DLineRenderer = CreateOrGetSubLineRenderer("Line_3DFrustum", frustumColor, floorLineWidth * 0.7f);
            }

            Vector3[] near = GetFrustumCornersAtDistance(origin, fwd, up, right, nearClip, tanV, tanH);
            Vector3[] far = GetFrustumCornersAtDistance(origin, fwd, up, right, farClip, tanV, tanH);

            Vector3[] path3D = new Vector3[]
            {
                origin, near[0], far[0], far[1], near[1], origin,
                near[2], far[2], far[3], near[3], origin,
                near[0], near[3], far[3], far[0], far[1], far[2], near[2], near[1], near[0]
            };

            frustum3DLineRenderer.enabled = true;
            frustum3DLineRenderer.positionCount = path3D.Length;
            frustum3DLineRenderer.SetPositions(path3D);
        }
        else if (frustum3DLineRenderer != null)
        {
            frustum3DLineRenderer.enabled = false;
        }

        // 2. Floor Footprint Sector & Vertical Gradient Wall in Game View
        Vector3 floorOrigin = new Vector3(origin.x, floorY, origin.z);
        Vector3 fwdXZ = new Vector3(fwd.x, 0f, fwd.z).normalized;
        if (fwdXZ.sqrMagnitude < 0.001f) fwdXZ = Vector3.forward;
        float halfHAngle = (horizontalFOV * 0.5f);

        if (showFloorInGameView && showFloorFootprint)
        {
            if (floorSectorLineRenderer == null)
            {
                floorSectorLineRenderer = CreateOrGetSubLineRenderer("Line_FloorFootprint", floorFootprintColor, floorLineWidth);
            }

            floorSectorLineRenderer.enabled = true;
            floorSectorLineRenderer.startColor = floorFootprintColor;
            floorSectorLineRenderer.endColor = floorFootprintColor;

            int arcSteps = 24;
            List<Vector3> sectorPoints = new List<Vector3>();

            float stepAngle = horizontalFOV / arcSteps;
            for (int i = 0; i <= arcSteps; i++)
            {
                float ang = -halfHAngle + i * stepAngle;
                sectorPoints.Add(floorOrigin + Quaternion.Euler(0f, ang, 0f) * fwdXZ * nearClip);
            }

            for (int i = arcSteps; i >= 0; i--)
            {
                float ang = -halfHAngle + i * stepAngle;
                sectorPoints.Add(floorOrigin + Quaternion.Euler(0f, ang, 0f) * fwdXZ * farClip);
            }

            sectorPoints.Add(sectorPoints[0]);

            floorSectorLineRenderer.positionCount = sectorPoints.Count;
            floorSectorLineRenderer.SetPositions(sectorPoints.ToArray());

            if (enableVerticalGradientWall)
            {
                if (floorCurtainMeshFilter == null)
                {
                    MeshRenderer mr;
                    floorCurtainMeshFilter = CreateOrGetSubMeshFilter("Wall_FloorFootprintCurtain", out mr);
                    floorCurtainMesh = new Mesh();
                    floorCurtainMesh.name = "FloorCurtainMesh";
                    floorCurtainMeshFilter.sharedMesh = floorCurtainMesh;
                }

                floorCurtainMeshFilter.gameObject.SetActive(true);
                UpdateCurtainMesh(floorCurtainMesh, sectorPoints, floorFootprintColor, wallHeight, wallBaseAlpha);
            }
            else if (floorCurtainMeshFilter != null)
            {
                floorCurtainMeshFilter.gameObject.SetActive(false);
            }
        }
        else
        {
            if (floorSectorLineRenderer != null) floorSectorLineRenderer.enabled = false;
            if (floorCurtainMeshFilter != null) floorCurtainMeshFilter.gameObject.SetActive(false);
        }

        // 3. Radius Distance Rings on Floor & Optional Ring Walls
        if (showFloorInGameView && showRadiusRings)
        {
            List<float> ringDistances = new List<float>();
            for (float r = radiusRingInterval; r < farClip; r += radiusRingInterval)
            {
                if (r > nearClip + 0.1f)
                {
                    ringDistances.Add(r);
                }
            }

            while (radiusRingRenderers.Count < ringDistances.Count)
            {
                int index = radiusRingRenderers.Count;
                LineRenderer lr = CreateOrGetSubLineRenderer($"Line_RadiusRing_{index}", radiusRingColor, floorLineWidth * 0.85f);
                radiusRingRenderers.Add(lr);
            }

            if (enableVerticalGradientWall && includeRadiusRingWalls)
            {
                while (ringCurtainMeshFilters.Count < ringDistances.Count)
                {
                    int index = ringCurtainMeshFilters.Count;
                    MeshRenderer mr;
                    MeshFilter mf = CreateOrGetSubMeshFilter($"Wall_RadiusRingCurtain_{index}", out mr);
                    Mesh m = new Mesh();
                    m.name = $"RingCurtainMesh_{index}";
                    mf.sharedMesh = m;
                    ringCurtainMeshFilters.Add(mf);
                    ringCurtainMeshes.Add(m);
                }
            }

            int arcSegments = 30;
            float step = horizontalFOV / arcSegments;

            for (int i = 0; i < radiusRingRenderers.Count; i++)
            {
                LineRenderer lr = radiusRingRenderers[i];
                if (i < ringDistances.Count)
                {
                    float currentRadius = ringDistances[i];
                    lr.enabled = true;
                    lr.startColor = radiusRingColor;
                    lr.endColor = radiusRingColor;

                    List<Vector3> arcPoints = new List<Vector3>(arcSegments + 1);
                    for (int s = 0; s <= arcSegments; s++)
                    {
                        float ang = -halfHAngle + s * step;
                        arcPoints.Add(floorOrigin + Quaternion.Euler(0f, ang, 0f) * fwdXZ * currentRadius);
                    }

                    lr.positionCount = arcPoints.Count;
                    lr.SetPositions(arcPoints.ToArray());

                    if (enableVerticalGradientWall && includeRadiusRingWalls && i < ringCurtainMeshFilters.Count)
                    {
                        ringCurtainMeshFilters[i].gameObject.SetActive(true);
                        UpdateCurtainMesh(ringCurtainMeshes[i], arcPoints, radiusRingColor, wallHeight * 0.6f, wallBaseAlpha * 0.7f);
                    }
                }
                else
                {
                    lr.enabled = false;
                    if (i < ringCurtainMeshFilters.Count) ringCurtainMeshFilters[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            foreach (var lr in radiusRingRenderers)
            {
                if (lr != null) lr.enabled = false;
            }
            foreach (var mf in ringCurtainMeshFilters)
            {
                if (mf != null) mf.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateCurtainMesh(Mesh mesh, List<Vector3> basePoints, Color baseColor, float height, float baseAlpha)
    {
        mesh.Clear();
        int count = basePoints.Count;
        if (count < 2) return;

        List<Vector3> verts = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> tris = new List<int>();

        Color botCol = new Color(baseColor.r, baseColor.g, baseColor.b, baseAlpha);
        Color topCol = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        for (int i = 0; i < count - 1; i++)
        {
            Vector3 p0 = basePoints[i];
            Vector3 p1 = basePoints[i + 1];

            int idx = verts.Count;

            verts.Add(p0);
            verts.Add(p0 + Vector3.up * height);
            verts.Add(p1);
            verts.Add(p1 + Vector3.up * height);

            colors.Add(botCol);
            colors.Add(topCol);
            colors.Add(botCol);
            colors.Add(topCol);

            tris.Add(idx + 0);
            tris.Add(idx + 1);
            tris.Add(idx + 2);

            tris.Add(idx + 2);
            tris.Add(idx + 1);
            tris.Add(idx + 3);

            tris.Add(idx + 2);
            tris.Add(idx + 1);
            tris.Add(idx + 0);

            tris.Add(idx + 3);
            tris.Add(idx + 1);
            tris.Add(idx + 2);
        }

        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void CleanupRuntimeVisuals()
    {
        if (runtimeVisualsContainer != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeVisualsContainer);
            }
            else
            {
                DestroyImmediate(runtimeVisualsContainer);
            }
            runtimeVisualsContainer = null;
        }

        if (floorCurtainMesh != null)
        {
            if (Application.isPlaying) Destroy(floorCurtainMesh);
            else DestroyImmediate(floorCurtainMesh);
            floorCurtainMesh = null;
        }

        foreach (var m in ringCurtainMeshes)
        {
            if (m != null)
            {
                if (Application.isPlaying) Destroy(m);
                else DestroyImmediate(m);
            }
        }

        ringCurtainMeshes.Clear();
        ringCurtainMeshFilters.Clear();
        radiusRingRenderers.Clear();
        frustum3DLineRenderer = null;
        floorSectorLineRenderer = null;
    }
}
