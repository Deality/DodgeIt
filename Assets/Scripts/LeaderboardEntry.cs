using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardEntry : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;

    [Header("Oyuncunun Kendi Satırı")]
    [Tooltip("Satırın çerçeve görseli. Boşsa bu objedeki Image kullanılır.")]
    public Image frameImage;
    [Tooltip("Oyuncunun kendi satırında kullanılan çerçeve (beyaz yerine koyu mavi kenarlı).")]
    public Sprite ownFrameSprite;

    private Sprite defaultFrameSprite;

    public void SetData(int rank, string playerName, int score, bool isOwn = false)
    {
        rankText.text = rank.ToString();
        nameText.text = playerName;
        scoreText.text = score.Dotted();

        if (frameImage == null) frameImage = GetComponent<Image>();
        if (frameImage != null)
        {
            if (defaultFrameSprite == null) defaultFrameSprite = frameImage.sprite;
            frameImage.sprite = isOwn && ownFrameSprite != null ? ownFrameSprite : defaultFrameSprite;
        }
    }
}
