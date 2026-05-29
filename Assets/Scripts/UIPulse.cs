using UnityEngine;

public class UIPulse : MonoBehaviour
{
    public float pulseAmount = 0.04f;
    public float pulseSpeed = 1.5f;

    private Vector3 startScale;
    private float randomOffset;

    void Awake()
    {
        startScale = transform.localScale;
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + randomOffset) * pulseAmount;
        transform.localScale = startScale * pulse;
    }
}