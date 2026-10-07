using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerLogger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string triggerName = "Trigger_1";

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Just tell the manager we arrived! It handles the rest.
            if (KinectSessionManager.Instance != null)
            {
                KinectSessionManager.Instance.TriggerEntered(triggerName);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Tell the manager we left
            if (KinectSessionManager.Instance != null)
            {
                KinectSessionManager.Instance.TriggerExited(triggerName);
            }
        }
    }
}