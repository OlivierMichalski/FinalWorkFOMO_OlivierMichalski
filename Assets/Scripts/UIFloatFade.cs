using UnityEngine;

public class UIFloatFade : MonoBehaviour
{
    [Header("Floating")]
    public float moveAmount = 10f;
    public float moveSpeed = 0.6f;
    public float rotateAmount = 1.5f;
    public float rotateSpeed = 0.5f;

    [Header("Fading")]
    public float minAlpha = 0.08f;
    public float maxAlpha = 0.35f;
    public float fadeSpeed = 0.7f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 startPosition;
    private Quaternion startRotation;
    private float randomOffset;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        startPosition = rectTransform.anchoredPosition;
        startRotation = rectTransform.localRotation;

        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * moveSpeed + randomOffset) * moveAmount;
        float xOffset = Mathf.Sin(Time.time * moveSpeed * 0.7f + randomOffset) * (moveAmount * 0.5f);
        float rotation = Mathf.Sin(Time.time * rotateSpeed + randomOffset) * rotateAmount;

        rectTransform.anchoredPosition = startPosition + new Vector2(xOffset, yOffset);
        rectTransform.localRotation = startRotation * Quaternion.Euler(0f, 0f, rotation);

        float fade = Mathf.Sin(Time.time * fadeSpeed + randomOffset);
        float normalizedFade = (fade + 1f) / 2f;

        canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, normalizedFade);
    }
}