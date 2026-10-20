using UnityEngine;
using UnityEngine.UI;

// Marketteki araba kartında, arabayı seçili yolun üstünde büyük gösteren önizlemeyi açan buton
// (PREVIEW butonu ve arabanın küçük görseli). Yol önizlemesiyle aynı paneli kullanır.
[RequireComponent(typeof(Button))]
public class CarPreviewButton : MonoBehaviour
{
    [Tooltip("Bu kartın ShopItem'ı. Görevle açılan araba hâlâ kilitliyse önizleme açılmaz (araba gizli kalsın).")]
    public ShopItem item;
    [Tooltip("Karttaki küçük araba görseli; önizlemede bunun sprite'ı büyütülür.")]
    public Image carImage;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Open);
    }

    void Open()
    {
        if (carImage == null || CarDatabase.instance == null) return;

        if (item != null && item.achievementOnly)
        {
            bool unlocked = PlayerPrefs.GetInt(item.itemType.ToString() + "_Purchased_" + item.itemIndex, 0) == 1;
            if (!unlocked) return;
        }

        Sprite road = CarDatabase.instance.GetRoad(PlayerPrefs.GetInt("Selected_Road", 0));
        var priceText = item != null ? item.priceText : null;
        RoadPreviewPanel.Show(road, carImage.canvas, priceText != null ? priceText.font : null, carImage.sprite);
    }
}
