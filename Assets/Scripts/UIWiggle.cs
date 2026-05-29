using UnityEngine;

public class UIWiggle : MonoBehaviour
{
    public float moveAmount = 5f;
    public float moveSpeed = 1.5f;

    public float rotateAmount = 1f;
    public float rotateSpeed = 1f;

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Quaternion startRotation;
    private float randomOffset;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
        startRotation = rectTransform.localRotation;
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * moveSpeed + randomOffset) * moveAmount;
        float rotation = Mathf.Sin(Time.time * rotateSpeed + randomOffset) * rotateAmount;

        rectTransform.anchoredPosition = startPosition + new Vector2(0f, yOffset);
        rectTransform.localRotation = startRotation * Quaternion.Euler(0f, 0f, rotation);
    }
}