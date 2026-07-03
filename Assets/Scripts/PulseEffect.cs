using UnityEngine;

public class PulseEffect : MonoBehaviour
{
    [Header("Nefes Alma Ayarları")]
    public float pulseSpeed = 5f; // Hız
    public float pulseAmount = 0.2f; // Büyüme miktarı

    private Vector3 initialScale;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {
        // Sinüs dalgası ile büyüyüp küçülme
        float scaleFactor = 1 + (Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
        transform.localScale = initialScale * scaleFactor;
    }
}