using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildCoinShopPanel
{
    const string CoinShopDir = "Assets/Arayüz Elementleri/MainMenüV2/CoinShop/";
    const string BackArrowPath = "Assets/Arayüz Elementleri/MainMenüV2/Shop/Varlık 15@2x.png";
    const string BackArrowSpriteName = "Varlık 15@2x_0";
    const string FontGuid = "5706648168ca9344f95625db1d6aa1bf";

    class Item
    {
        public string file, iconName, pillName, amount, price;
        public Vector2 pos;
    }

    [MenuItem("Tools/DodgeIt/Build Coin Shop Panel")]
    public static void Execute()
    {
        GameObject anchor = GameObject.Find("PowerUpShopPanel");
        if (anchor == null)
        {
            Debug.LogError("PowerUpShopPanel not found - open Assets/Scenes/MainMenu.unity first.");
            return;
        }
        Transform root = anchor.transform.parent;

        Transform existing = root.Find("CoinShopPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        UIManager uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));

        GameObject panel = new GameObject("CoinShopPanel", typeof(RectTransform), typeof(Image), typeof(CoinShopPanelAnimator));
        panel.transform.SetParent(root, false);
        SetFullScreenRect((RectTransform)panel.transform);
        Image dimImg = panel.GetComponent<Image>();
        dimImg.sprite = null;
        dimImg.color = new Color(0, 0, 0, 0.75f);
        dimImg.raycastTarget = true;

        GameObject box = new GameObject("CoinShopBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(panel.transform, false);
        SetRect((RectTransform)box.transform, Vector2.zero, new Vector2(1000, 1000));
        box.GetComponent<Image>().sprite = LoadSprite(CoinShopDir + "Varlık 90.png", "Varlık 90_0");

        CoinShopPanelAnimator animator = panel.GetComponent<CoinShopPanelAnimator>();
        animator.dimBackground = dimImg;
        animator.box = (RectTransform)box.transform;

        GameObject back = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(ButtonScaler));
        back.transform.SetParent(box.transform, false);
        SetRect((RectTransform)back.transform, new Vector2(-420, 450), new Vector2(110, 110));
        Image backImg = back.GetComponent<Image>();
        backImg.sprite = LoadSprite(BackArrowPath, BackArrowSpriteName);
        Button backBtn = back.GetComponent<Button>();
        backBtn.targetGraphic = backImg;
        ConfigureScaler(back.GetComponent<ButtonScaler>());
        if (uiManager != null) UnityEventTools.AddPersistentListener(backBtn.onClick, uiManager.CloseCoinShop);

        var items = new[]
        {
            new Item { file = "Varlık 93.png", iconName = "Varlık 93_0", pillName = "Varlık 93_1", amount = "100",  price = "$0.99", pos = new Vector2(-190, 200) },
            new Item { file = "Varlık 94.png", iconName = "Varlık 94_0", pillName = "Varlık 94_1", amount = "500",  price = "$2.99", pos = new Vector2(190, 200) },
            new Item { file = "Varlık 92.png", iconName = "Varlık 92_0", pillName = "Varlık 92_1", amount = "1200", price = "$4.99", pos = new Vector2(-190, -240) },
            new Item { file = "Varlık 91.png", iconName = "Varlık 91_0", pillName = "Varlık 91_1", amount = "3000", price = "$9.99", pos = new Vector2(190, -240) },
        };

        int idx = 0;
        foreach (var it in items)
        {
            idx++;
            GameObject slot = new GameObject($"CoinItem_{idx}", typeof(RectTransform));
            slot.transform.SetParent(box.transform, false);
            SetRect((RectTransform)slot.transform, it.pos, new Vector2(300, 400));

            GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(slot.transform, false);
            SetRect((RectTransform)icon.transform, new Vector2(0, 30), new Vector2(260, 300));
            Image iconImg = icon.GetComponent<Image>();
            iconImg.sprite = LoadSprite(CoinShopDir + it.file, it.iconName);
            iconImg.preserveAspect = true;

            CreateText(slot.transform, "AmountText", it.amount, new Vector2(0, 135), new Vector2(210, 70), 46, font, new Color(0.15f, 0.08f, 0f));

            GameObject buy = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(ButtonScaler));
            buy.transform.SetParent(slot.transform, false);
            SetRect((RectTransform)buy.transform, new Vector2(0, -160), new Vector2(220, 66));
            Image buyImg = buy.GetComponent<Image>();
            buyImg.sprite = LoadSprite(CoinShopDir + it.file, it.pillName);
            buyImg.preserveAspect = true;
            Button buyBtn = buy.GetComponent<Button>();
            buyBtn.targetGraphic = buyImg;
            ConfigureScaler(buy.GetComponent<ButtonScaler>());

            CreateText(buy.transform, "PriceText", it.price, Vector2.zero, new Vector2(200, 60), 32, font, Color.white);
        }

        if (uiManager != null)
        {
            uiManager.coinShopPanel = panel;
            EditorUtility.SetDirty(uiManager);
        }

        GameObject coinMarket = GameObject.Find("CoinMarket");
        if (coinMarket != null && coinMarket.GetComponent<Button>() == null && uiManager != null)
        {
            Button cmBtn = coinMarket.AddComponent<Button>();
            cmBtn.targetGraphic = coinMarket.GetComponent<Image>();
            ConfigureScaler(coinMarket.AddComponent<ButtonScaler>());
            UnityEventTools.AddPersistentListener(cmBtn.onClick, uiManager.OpenCoinShop);
            EditorUtility.SetDirty(coinMarket);
        }

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(box);
        EditorSceneManager.MarkSceneDirty(panel.scene);
        Debug.Log("CoinShopPanel built and wired to UIManager.coinShopPanel.");
    }

    static void SetRect(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    static void SetFullScreenRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    static void ConfigureScaler(ButtonScaler s)
    {
        s.pressedScale = 0.9f;
        s.animationDuration = 0.1f;
        s.playSound = true;
        s.useHaptic = false;
    }

    static Sprite LoadSprite(string path, string spriteName)
    {
        foreach (Object a in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (a is Sprite s && s.name == spriteName) return s;
        }
        Debug.LogError($"Sprite '{spriteName}' not found at {path}");
        return null;
    }

    static void CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size, float fontSize, TMP_FontAsset font, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, pos, size);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
    }
}
