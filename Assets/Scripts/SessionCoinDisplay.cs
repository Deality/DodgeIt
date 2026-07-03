using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SessionCoinDisplay : MonoBehaviour
{
    [Header("Hedef Yazı")]
    [Tooltip("Toplam paranın yazdığı ANA yazıyı buraya sürükle.")]
    public TextMeshProUGUI totalCoinText;

    [Header("Animasyon Ayarları")]
    public float panelWaitTime = 0.5f; // Panelin inmesini bekle
    public float moveDuration = 0.5f;  // Uçma süresi
    public float countDuration = 0.8f; // Sayaç artış süresi

    private TextMeshProUGUI myText;
    private Vector3 originalLocalPos;
    private Color originalColor;
    private Coroutine animRoutine;

    private int targetTotalCoins;

    void Awake()
    {
        myText = GetComponent<TextMeshProUGUI>();
        // Yazının senin koyduğun ilk yerini ve rengini hafızaya kazı
        originalLocalPos = transform.localPosition;
        originalColor = myText.color;
    }

    void OnEnable()
    {
        // Paneli her açtığımızda yazıyı ilk haline sıfırla
        transform.localPosition = originalLocalPos;
        myText.color = originalColor;

        if (GameManager.instance == null || totalCoinText == null) return;

        int earned = GameManager.instance.sessionGems;
        targetTotalCoins = PlayerPrefs.GetInt("GemsCount", 0);

        // 🔥 GÜVENLİK 1: Okur okumaz GameManager'daki parayı SIFIRLA ki turlara asla sarkmasın!
        GameManager.instance.sessionGems = 0;

        // AutoCoinDisplay varsa çakışmasın diye sustur
        MonoBehaviour auto = totalCoinText.GetComponent("AutoCoinDisplay") as MonoBehaviour;
        if (auto != null) auto.enabled = false;

        if (earned > 0)
        {
            myText.text = "+" + earned.ToString();
            animRoutine = StartCoroutine(AnimRoutine(earned));
        }
        else
        {
            // Hiç para toplanmadıysa
            myText.text = "+0";
            myText.color = Color.gray;
            totalCoinText.text = targetTotalCoins.ToString();
        }
    }

    void OnDisable()
    {
        // 🔥 GÜVENLİK 2: Panel animasyon bitmeden kapanırsa (Restart atılırsa) her şeyi anında sonuca bağla
        if (animRoutine != null) StopCoroutine(animRoutine);

        transform.localPosition = originalLocalPos;
        myText.color = originalColor;

        if (totalCoinText != null)
            totalCoinText.text = targetTotalCoins.ToString();
    }

    IEnumerator AnimRoutine(int earnedAmount)
    {
        // Başlangıç parası (Yeni toplananlar çıkarılmış hali)
        int startTotal = targetTotalCoins - earnedAmount;
        totalCoinText.text = startTotal.ToString();

        // 1. Panelin aşağı tam inmesini bekle
        yield return new WaitForSecondsRealtime(panelWaitTime);

        // 2. Ana yazıya doğru uç ve şeffaflaş
        Vector3 startPos = transform.position;
        Vector3 endPos = totalCoinText.transform.position;
        float t = 0;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / moveDuration;
            float ease = t * t; // Yavaş başlayıp hızlanarak (Ease-In)

            // Pozisyonu kaydır
            transform.position = Vector3.Lerp(startPos, endPos, ease);

            // Alphasını (Şeffaflığı) düşür
            Color c = myText.color;
            c.a = Mathf.Lerp(originalColor.a, 0f, ease);
            myText.color = c;

            yield return null;
        }

        // 3. Sayacı artır
        t = 0;
        // Ana yazıya hafif bir patlama/büyüme hissi ver
        totalCoinText.rectTransform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / countDuration;

            // Sayıları yuvarlayarak artır
            int current = Mathf.RoundToInt(Mathf.Lerp(startTotal, targetTotalCoins, t));
            totalCoinText.text = current.ToString();

            // Ana yazıyı normal boyutuna geri küçült
            float scale = Mathf.Lerp(1.2f, 1f, t);
            totalCoinText.rectTransform.localScale = new Vector3(scale, scale, scale);

            yield return null;
        }

        // 4. Animasyon bittiğinde son değeri garantile
        totalCoinText.text = targetTotalCoins.ToString();
        totalCoinText.rectTransform.localScale = Vector3.one;
    }
}