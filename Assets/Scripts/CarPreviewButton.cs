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

        // Araba, oyundaki prefab sprite'ı ve yola göre oyundaki boyutuyla gösterilir
        Sprite carSprite = carImage.sprite;
        float ratio = 0.12f;
        if (item != null)
        {
            GameObject prefab = CarDatabase.instance.GetCar(item.itemIndex);
            var sr = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
            if (sr != null && sr.sprite != null) carSprite = sr.sprite;
            float r = CarDatabase.instance.CarWidthOnRoad(item.itemIndex, road);
            if (r > 0f) ratio = r;
        }
        RoadPreviewPanel.Show(road, carImage.canvas, priceText != null ? priceText.font : null, carSprite, ratio);
    }
}
