using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardEntry : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    [Tooltip("Adın altındaki #numara; ad ne kadar uzun olursa olsun boyutu sabittir.")]
    public TextMeshProUGUI tagText;
    public TextMeshProUGUI scoreText;

    [Header("Oyuncunun Kendi Satırı")]
    [Tooltip("Satırın çerçeve görseli. Boşsa bu objedeki Image kullanılır.")]
    public Image frameImage;
    [Tooltip("Oyuncunun kendi satırında kullanılan çerçeve (beyaz yerine koyu mavi kenarlı).")]
    public Sprite ownFrameSprite;

    private Sprite defaultFrameSprite;

    public void SetData(int rank, string playerName, int score, bool isOwn = false, string playerTag = "")
    {
        rankText.text = rank.ToString();
        nameText.text = playerName;
        if (tagText != null) tagText.text = playerTag;
        scoreText.text = score.Dotted();

        if (frameImage == null) frameImage = GetComponent<Image>();
        if (frameImage != null)
        {
            if (defaultFrameSprite == null) defaultFrameSprite = frameImage.sprite;
            frameImage.sprite = isOwn && ownFrameSprite != null ? ownFrameSprite : defaultFrameSprite;
        }
    }
}
