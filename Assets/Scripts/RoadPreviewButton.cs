using UnityEngine;
using UnityEngine.UI;

// Marketteki yol kartında, yolun büyük önizlemesini açan buton (PREVIEW butonu ve yolun küçük görseli).
[RequireComponent(typeof(Button))]
public class RoadPreviewButton : MonoBehaviour
{
    [Tooltip("Bu kartın ShopItem'ı. Görevle açılan yol hâlâ kilitliyse önizleme açılmaz (yol gizli kalsın).")]
    public ShopItem item;
    [Tooltip("Karttaki küçük yol görseli; önizlemede bunun sprite'ı büyütülür.")]
    public Image roadImage;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Open);
    }

    void Open()
    {
        if (roadImage == null) return;

        if (item != null && item.achievementOnly)
        {
            bool unlocked = PlayerPrefs.GetInt(item.itemType.ToString() + "_Purchased_" + item.itemIndex, 0) == 1;
            if (!unlocked) return;
        }

        var priceText = item != null ? item.priceText : null;
        RoadPreviewPanel.Show(roadImage.sprite, roadImage.canvas, priceText != null ? priceText.font : null);
    }
}
