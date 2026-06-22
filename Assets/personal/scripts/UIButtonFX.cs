using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class UIButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Main Button Transparency")]
    [Range(0f, 1f)]
    [Tooltip("The transparency of the button when it is just sitting idle.")]
    public float idleAlpha = 0.4f;

    [Range(0.05f, 1f)]
    [Tooltip("How many seconds the fade animation takes to finish.")]
    public float fadeDuration = 0.15f;

    [Header("Hover Glow Settings")]
    [Tooltip("Drag a secondary UI Image component here that represents your button's glow outline/halo.")]
    public Image glowImage;

    [Range(0f, 1f)]
    [Tooltip("The maximum opacity the glow image reaches when hovered.")]
    public float maxGlowAlpha = 1.0f;

    [Header("Size Settings")]
    [Tooltip("How much the button sizes up when clicked (e.g., 1.1 means it grows by 10%).")]
    public float pressScaleMultiplier = 1.1f;

    [Range(0.05f, 1f)]
    [Tooltip("How many seconds the scale up/down animation takes to finish.")]
    public float scaleDuration = 0.1f;

    private Image mainButtonImage;
    private Vector3 originalScale;
    private bool isHovered = false;

    private Coroutine mainFadeCoroutine;
    private Coroutine glowFadeCoroutine;
    private Coroutine scaleCoroutine;

    void Start()
    {
        mainButtonImage = GetComponent<Image>();
        originalScale = transform.localScale;

        if (mainButtonImage == null)
        {
            Debug.LogError($"[UIButtonEffects] Missing an Image component on {gameObject.name}! Place this script on a UI Object with an Image.");
        }

        ResetToIdle();
    }

    void OnValidate()
    {
        // Live Editor updates when you adjust the sliders
        if (mainButtonImage == null) mainButtonImage = GetComponent<Image>();
        if (mainButtonImage != null && !Application.isPlaying)
        {
            Color c = mainButtonImage.color;
            c.a = idleAlpha;
            mainButtonImage.color = c;
        }

        if (glowImage != null && !Application.isPlaying)
        {
            Color c = glowImage.color;
            c.a = 0f; // Keep it invisible by default in editor design phase
            glowImage.color = c;
        }
    }

    // 1. Mouse Hover Entered -> Fade button to full visibility (1.0) & Fade Glow in
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        StartSmoothFade(ref mainFadeCoroutine, mainButtonImage, 1.0f);
        StartSmoothFade(ref glowFadeCoroutine, glowImage, maxGlowAlpha);
    }

    // 2. Mouse Hover Exited -> Fade button back to idle baseline & Fade Glow completely out (0.0)
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        StartSmoothFade(ref mainFadeCoroutine, mainButtonImage, idleAlpha);
        StartSmoothFade(ref glowFadeCoroutine, glowImage, 0f);

        StartSmoothScale(originalScale);
    }

    // 3. Mouse Down Pressed -> Smoothly scale up
    public void OnPointerDown(PointerEventData eventData)
    {
        StartSmoothScale(originalScale * pressScaleMultiplier);
    }

    // 4. Mouse Up Released -> Smoothly return back to baseline scale
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isHovered)
        {
            StartSmoothScale(originalScale);
        }
    }

    private void StartSmoothFade(ref Coroutine activeCoroutine, Image targetImage, float targetAlpha)
    {
        if (targetImage == null) return;

        if (activeCoroutine != null) StopCoroutine(activeCoroutine);

        if (gameObject.activeInHierarchy)
        {
            activeCoroutine = StartCoroutine(AnimateFade(targetImage, targetAlpha));
        }
    }

    private IEnumerator AnimateFade(Image img, float targetAlpha)
    {
        float startAlpha = img.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            Color currentColor = img.color;
            currentColor.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            img.color = currentColor;
            yield return null;
        }

        Color finalColor = img.color;
        finalColor.a = targetAlpha;
        img.color = finalColor;
    }

    private void StartSmoothScale(Vector3 targetScale)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);

        if (gameObject.activeInHierarchy)
        {
            scaleCoroutine = StartCoroutine(AnimateScale(targetScale));
        }
    }

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / scaleDuration);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void ResetToIdle()
    {
        if (mainButtonImage != null)
        {
            Color c = mainButtonImage.color;
            c.a = idleAlpha;
            mainButtonImage.color = c;
        }

        if (glowImage != null)
        {
            Color c = glowImage.color;
            c.a = 0f;
            glowImage.color = c;
        }

        transform.localScale = originalScale;
    }
}