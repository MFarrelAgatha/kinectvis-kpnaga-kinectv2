using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class KinectTriggerManager : MonoBehaviour
{
    public enum PostureRequirement { AnyState, SittingOnly, StandingOnly }

    [System.Serializable]
    public class TriggerSetting
    {
        [Header("Physical Trigger Object")]
        [Tooltip("Drag the 3D Object or Collider from your scene that acts as this zone.")]
        public Collider zoneCollider;

        [Header("UI Message Overlay Settings")]
        [Tooltip("Drag the specific UI canvas panel for this trigger zone here.")]
        public GameObject messagePanel;

        [Tooltip("Drag the TextMeshPro text component inside that UI panel here.")]
        public TMP_Text panelTextField;

        [TextArea(3, 6)]
        [Tooltip("Type the unique message you want this panel to show.")]
        public string inspectorMessage = "Welcome to the custom interaction zone!";

        [Header("Audio Settings")]
        [Tooltip("Optional: Drop a unique sound effect file here to play when triggered.")]
        public AudioClip triggerSFX;

        [Tooltip("Should the audio loop continuously while the user satisfies the condition inside the zone?")]
        public bool loopAudio = false;

        [Range(0.1f, 5.0f)]
        [Tooltip("How many seconds should the audio take to smoothly fade out when exiting or breaking posture?")]
        public float fadeOutDuration = 1.0f;

        [Header("Required Verification Rules")]
        [Tooltip("What state must the user hold to trigger this specific zone event?")]
        public PostureRequirement postureRequirement = PostureRequirement.AnyState;

        [Tooltip("How many continuous seconds must the user remain in this state inside the zone before the event fires?")]
        public float continuousDurationRequired = 2.0f;

        // Runtime states managed internally
        [HideInInspector] public bool isUserInside = false;
        [HideInInspector] public float conditionTimer = 0f;
        [HideInInspector] public bool eventHasFired = false;
        [HideInInspector] public AudioSource audioSource;
        [HideInInspector] public Coroutine fadeCoroutine;
    }

    [Header("Global Trigger Configurations List")]
    [Tooltip("Click the '+' button to add as many customizable trigger items as you want!")]
    public List<TriggerSetting> triggerZones = new List<TriggerSetting>();

    void Start()
    {
        foreach (TriggerSetting zone in triggerZones)
        {
            if (zone.zoneCollider == null)
            {
                Debug.LogWarning($"[TriggerManager] Entry missing its 'Zone Collider' reference on: {gameObject.name}");
                continue;
            }

            zone.zoneCollider.isTrigger = true;

            // DYNAMIC HYBRID AUDIO: Creates an isolated 2D channel right on this manager per zone item
            zone.audioSource = gameObject.AddComponent<AudioSource>();
            zone.audioSource.playOnAwake = false;
            zone.audioSource.spatialBlend = 0f; // Hard-coded to 2D for loud, clear room-wide sound
            zone.audioSource.clip = zone.triggerSFX;
            zone.audioSource.loop = zone.loopAudio;

            if (zone.messagePanel != null) zone.messagePanel.SetActive(false);

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

        foreach (TriggerSetting zone in triggerZones)
        {
            if (!zone.isUserInside) continue;

            // Evaluate if user is currently matching the posture rule criteria
            bool isConditionValid = false;
            switch (zone.postureRequirement)
            {
                case PostureRequirement.AnyState:
                    isConditionValid = (activeKinectState == "Sitting" || activeKinectState == "Standing");
                    break;
                case PostureRequirement.SittingOnly:
                    isConditionValid = (activeKinectState == "Sitting");
                    break;
                case PostureRequirement.StandingOnly:
                    isConditionValid = (activeKinectState == "Standing");
                    break;
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

    private void FireZoneEvent(TriggerSetting zone)
    {
        zone.eventHasFired = true;

        // 1. Alert main logger pipeline to append lines to your CSV file
        KinectSessionManager.Instance.TriggerEntered(zone.zoneCollider.gameObject.name);

        // 2. Display the UI popover message overlay
        if (zone.messagePanel != null)
        {
            zone.messagePanel.SetActive(true);
            if (zone.panelTextField != null) zone.panelTextField.text = zone.inspectorMessage;
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

        // 1. Instantly hide UI panels when terms break
        if (zone.messagePanel != null) zone.messagePanel.SetActive(false);

        // 2. Safely communicate exit to master log loops
        KinectSessionManager.Instance.TriggerExited(zone.zoneCollider.gameObject.name);

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
        return other.CompareTag("Player") || other.name.Contains("Joint") || other.name.Contains("Avatar");
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