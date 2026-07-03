using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
[RequireComponent(typeof(Animator))]
public class ToggleAnimasyon : MonoBehaviour
{
    private Toggle toggle;
    private Animator animator;

    void Awake()
    {
        // Referansları Awake'te alıyoruz ki her seferinde aramasın
        toggle = GetComponent<Toggle>();
        animator = GetComponent<Animator>();

        // Listener'ı bir kez eklemek yeterli
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    // 🔥 ÇÖZÜM BURADA: Panel her açıldığında (Enable olduğunda) çalışır
    void OnEnable()
    {
        if (animator != null && toggle != null)
        {
            // 1. Parametreyi güncelle
            animator.SetBool("IsOn", toggle.isOn);

            // 2. Animasyonu beklemeden direkt son haline ışınla (Snap)
            // Böylece panel açılınca "kayma" efekti görmezsin, buton olması gereken yerde durur.
            // Animasyon isimlerinin "ToggleOn" ve "ToggleOff" olduğunu varsayıyorum.
            if (toggle.isOn)
            {
                animator.Play("ToggleOn", 0, 1.0f); // 1.0f = Animasyonun son karesi
            }
            else
            {
                animator.Play("ToggleOff", 0, 1.0f);
            }
        }
    }

    void OnToggleValueChanged(bool isOn)
    {
        if (animator != null)
        {
            // Tıklayınca normal animasyon oynasın
            animator.SetBool("IsOn", isOn);
        }
    }
}