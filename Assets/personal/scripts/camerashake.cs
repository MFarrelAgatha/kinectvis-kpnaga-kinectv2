using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Toggle Switch")]
    [Tooltip("Turn camera shake on or off completely.")]
    public bool isShakeEnabled = true;

    [Header("Shake Intensity")]
    [Range(0f, 5f)]
    [Tooltip("How violent the shake is.")]
    public float intensity = 1.0f;

    [Header("Shake Frequencies")]
    [Range(0.1f, 20f)]
    [Tooltip("How fast the camera moves back and forth horizontally.")]
    public float xFrequency = 5.0f;

    [Range(0.1f, 20f)]
    [Tooltip("How fast the camera moves up and down vertically.")]
    public float yFrequency = 5.0f;

    private Vector3 originalPosition;
    private float seedX;
    private float seedY;

    void OnEnable()
    {
        // Save the starting position so we don't permanently drift away
        originalPosition = transform.localPosition;

        // Unique random seeds for X and Y so they don't look completely identical
        seedX = Random.value * 100f;
        seedY = Random.value * 100f;
    }

    void Update()
    {
        // If the switch is turned off, smoothly return to normal and stop
        if (!isShakeEnabled || intensity <= 0)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * 5f);
            return;
        }

        // Calculate smooth random offsets using Perlin Noise based on independent X and Y frequencies
        float timeValueX = Time.time * xFrequency + seedX;
        float timeValueY = Time.time * yFrequency + seedY;

        // PerlinNoise returns 0 to 1, subtracting 0.5 centers it between -0.5 and 0.5
        float offsetX = (Mathf.PerlinNoise(timeValueX, 0f) - 0.5f) * intensity;
        float offsetY = (Mathf.PerlinNoise(0f, timeValueY) - 0.5f) * intensity;

        // Apply the independent offsets to the camera's original starting position
        transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);
    }

    void OnDisable()
    {
        // Ensure the camera resets perfectly when the script is disabled
        transform.localPosition = originalPosition;
    }
}