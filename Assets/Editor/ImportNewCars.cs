using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// Masaüstündeki yeni araba görsellerini oyuna ekler:
//  - Engel arabaları  -> prefab + ObstacleManager.obstaclePrefabs (SampleScene)
//  - Oyuncu arabaları -> prefab + CarDatabase (SampleScene & MainMenu) + Market kartı (MainMenu)
// Yeni prefablar mevcut arabalardan kopyalanır, böylece tüm ayarları (hız, efektler, collider vb.) aynı kalır.
// Tekrar çalıştırmak güvenlidir: zaten eklenmiş olanlar atlanır.
public static class ImportNewCars
{
    const string SourceRoot = @"C:\Users\Administrator\Desktop\DodgeIt-UI";
    const string SpriteFolder = "Assets/Prefabs/ArabalarPNG/Yeni";

    const string PlayerTemplatePath = "Assets/Prefabs/Player Cars/O.Arabası 8.prefab";
    const string ObstacleTemplatePath = "Assets/Prefabs/Enemy Cars/Araba 6.prefab";
    const string PlayerPrefabFolder = "Assets/Prefabs/Player Cars";
    const string ObstaclePrefabFolder = "Assets/Prefabs/Enemy Cars";

    const string GameScenePath = "Assets/Scenes/SampleScene.unity";
    const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    // Mevcut fiyat dizisi 500'er artıyor (0, 500, 1000 ... 4000); yeni arabalar aynı şekilde devam eder.
    const int PriceStep = 500;

    static readonly string[] ObstacleSources =
    {
        @"Obstacle Cars\1x\Varlık 20.png",
        @"Obstacle Cars\1x\Varlık 21.png",
        @"Obstacle Cars\1x\Varlık 23.png",
        @"Obstacle Cars\1x\Üretken Nesne.png",
    };

    static readonly string[] PlayerSources =
    {
        @"Player Cars\1x\Üretken Nesne (2).png",
        @"Player Cars\1x\Üretken Nesne (3).png",
        @"Player Cars\1x\Üretken Nesne (4).png",
        @"Player Cars\1x\Üretken Nesne (5).png",
        @"Player Cars\1x\Üretken Nesne (6).png",
        @"Player Cars\1x\Üretken Nesne (7).png",
        @"Player Cars\1x\Üretken Nesne (8).png",
        @"Player Cars\1x\Üretken Nesne (9).png",
        @"Player Cars\1x\Üretken Nesne (10).png",
        @"Player Cars\1x\Varlık 32.png",
    };

    [MenuItem("Tools/DodgeIt/Import New Cars")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previousScene = EditorSceneManager.GetActiveScene().path;

        GameObject playerTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerTemplatePath);
        GameObject obstacleTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(ObstacleTemplatePath);
        if (playerTemplate == null || obstacleTemplate == null)
        {
            Debug.LogError("ImportNewCars: şablon prefab bulunamadı.");
            return;
        }

        // Şablon prefabın kullandığı mevcut ArabalarPNG görselinin import ayarlarını baz al
        Sprite playerTemplateSprite = GetCarRenderer(playerTemplate).sprite;
        Sprite obstacleTemplateSprite = GetCarRenderer(obstacleTemplate).sprite;

        // 1. GÖRSELLER
        Directory.CreateDirectory(SpriteFolder);
        var obstacleSprites = new List<Sprite>();
        for (int i = 0; i < ObstacleSources.Length; i++)
            obstacleSprites.Add(ImportSprite(ObstacleSources[i], $"Engel {7 + i}.png", obstacleTemplateSprite));

        var playerSprites = new List<Sprite>();
        for (int i = 0; i < PlayerSources.Length; i++)
            playerSprites.Add(ImportSprite(PlayerSources[i], $"Oyuncu {9 + i}.png", playerTemplateSprite));

        if (obstacleSprites.Contains(null) || playerSprites.Contains(null))
        {
            Debug.LogError("ImportNewCars: bazı görseller yüklenemedi, işlem durduruldu.");
            return;
        }

        // 2. PREFABLAR
        var obstaclePrefabs = new List<GameObject>();
        for (int i = 0; i < obstacleSprites.Count; i++)
            obstaclePrefabs.Add(CreateCarPrefab(ObstacleTemplatePath, $"{ObstaclePrefabFolder}/Araba {7 + i}.prefab", obstacleSprites[i]));

        var playerPrefabs = new List<GameObject>();
        for (int i = 0; i < playerSprites.Count; i++)
            playerPrefabs.Add(CreateCarPrefab(PlayerTemplatePath, $"{PlayerPrefabFolder}/O.Arabası {9 + i}.prefab", playerSprites[i]));

        // 3. OYUN SAHNESİ
        var gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        foreach (var om in FindInScene<ObstacleManager>(gameScene))
        {
            foreach (var p in obstaclePrefabs)
                if (!om.obstaclePrefabs.Contains(p)) om.obstaclePrefabs.Add(p);
            EditorUtility.SetDirty(om);
        }
        AddToCarDatabases(gameScene, playerPrefabs);
        EditorSceneManager.SaveScene(gameScene);

        // 4. ANA MENÜ (Market)
        var menuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        AddToCarDatabases(menuScene, playerPrefabs);
        AddShopEntries(menuScene, playerPrefabs, playerSprites);
        EditorSceneManager.SaveScene(menuScene);

        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(previousScene) && previousScene != MainMenuScenePath)
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);

        Debug.Log($"ImportNewCars: {obstaclePrefabs.Count} engel ve {playerPrefabs.Count} oyuncu arabası eklendi.");
    }

    // --- GÖRSEL ---

    static Sprite ImportSprite(string relativeSource, string fileName, Sprite templateSprite)
    {
        string src = Path.Combine(SourceRoot, relativeSource);
        string dst = $"{SpriteFolder}/{fileName}";

        if (!File.Exists(dst))
        {
            if (!File.Exists(src))
            {
                Debug.LogError($"ImportNewCars: kaynak görsel yok: {src}");
                return null;
            }
            File.Copy(src, dst);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(dst);
        var templateImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(templateSprite));

        var settings = new TextureImporterSettings();
        templateImporter.ReadTextureSettings(settings);
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)SpriteAlignment.Center;

        // Yeni görsel daha düşük çözünürlükte olabilir; PPU'yu, sprite'ın dünya yüksekliği
        // şablon arabayla aynı olacak şekilde ayarla (prefab ölçekleri değişmeden kalır).
        importer.GetSourceTextureWidthAndHeight(out int _, out int height);
        float templateHeightUnits = templateSprite.rect.height / templateSprite.pixelsPerUnit;
        settings.spritePixelsPerUnit = height / templateHeightUnits;

        importer.SetTextureSettings(settings);
        importer.textureType = TextureImporterType.Sprite;
        importer.textureCompression = templateImporter.textureCompression;
        importer.maxTextureSize = templateImporter.maxTextureSize;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(dst);
    }

    // --- PREFAB ---

    static GameObject CreateCarPrefab(string templatePath, string newPath, Sprite sprite)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
        if (existing != null) return existing;

        AssetDatabase.CopyAsset(templatePath, newPath);
        GameObject root = PrefabUtility.LoadPrefabContents(newPath);

        root.name = Path.GetFileNameWithoutExtension(newPath);

        SpriteRenderer sr = GetCarRenderer(root);
        Vector2 oldSize = sr.sprite.bounds.size;
        sr.sprite = sprite;
        if (sr.gameObject != root) sr.gameObject.name = sprite.name;
        Vector2 newSize = sprite.bounds.size;
        var ratio = new Vector2(newSize.x / oldSize.x, newSize.y / oldSize.y);

        // Collider'ı yeni arabanın boyutuna göre ölçekle (şablondaki kenar payı korunur)
        var box = root.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = Vector2.Scale(box.size, ratio);
            box.offset = Vector2.Scale(box.offset, ratio);
        }

        var poly = root.GetComponent<PolygonCollider2D>();
        if (poly != null)
        {
            for (int p = 0; p < poly.pathCount; p++)
                poly.SetPath(p, poly.GetPath(p).Select(v => Vector2.Scale(v, ratio)).ToArray());
        }

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, newPath);
        PrefabUtility.UnloadPrefabContents(root);
        return saved;
    }

    // Araba gövdesini çizen renderer: oyuncu arabalarında kök objede, engellerde doğrudan
    // alt objede durur (gölge de alt objededir ama sortingOrder'ı daha düşüktür)
    static SpriteRenderer GetCarRenderer(GameObject go)
    {
        var rootRenderer = go.GetComponent<SpriteRenderer>();
        if (rootRenderer != null && rootRenderer.sprite != null) return rootRenderer;

        return go.transform.Cast<Transform>()
                 .Select(t => t.GetComponent<SpriteRenderer>())
                 .Where(r => r != null && r.sprite != null)
                 .OrderByDescending(r => r.sortingOrder)
                 .First();
    }

    // --- SAHNE ---

    static IEnumerable<T> FindInScene<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));
    }

    static void AddToCarDatabases(UnityEngine.SceneManagement.Scene scene, List<GameObject> cars)
    {
        foreach (var db in FindInScene<CarDatabase>(scene))
        {
            var list = db.carPrefabs.ToList();
            foreach (var c in cars)
                if (!list.Contains(c)) list.Add(c);
            db.carPrefabs = list.ToArray();
            EditorUtility.SetDirty(db);
        }
    }

    static void AddShopEntries(UnityEngine.SceneManagement.Scene scene, List<GameObject> cars, List<Sprite> sprites)
    {
        var carButtons = FindInScene<ShopItem>(scene).Where(s => s.itemType == MarketItemType.Car).ToList();
        if (carButtons.Count == 0)
        {
            Debug.LogError("ImportNewCars: MainMenu'de araba ShopItem bulunamadı.");
            return;
        }

        var market = FindInScene<MarketManager>(scene).FirstOrDefault();
        CarDatabase db = FindInScene<CarDatabase>(scene).FirstOrDefault();

        foreach (var car in cars)
        {
            int index = db != null ? System.Array.IndexOf(db.carPrefabs, car) : -1;
            if (index < 0) continue;
            if (carButtons.Any(b => b.itemIndex == index)) continue; // zaten eklenmiş

            ShopItem last = carButtons.OrderBy(b => b.itemIndex).Last();
            int price = last.price + PriceStep;

            // Kart = "Araba (N)" objesi; içinde "Araba" (görsel) ve "Fiyat" (ShopItem butonu) var
            Transform templateCard = last.transform.parent;
            GameObject card = Object.Instantiate(templateCard.gameObject, templateCard.parent);
            card.name = $"Araba - {car.name}";
            card.transform.SetAsLastSibling();

            Image carImage = card.transform.Find("Araba")?.GetComponent<Image>();
            if (carImage != null) carImage.sprite = sprites[cars.IndexOf(car)];

            ShopItem shopItem = card.GetComponentInChildren<ShopItem>(true);
            shopItem.itemIndex = index;
            shopItem.price = price;
            carButtons.Add(shopItem);

            if (market != null && !market.carItems.Any(m => m.carPrefab == car))
            {
                market.carItems.Add(new MarketItemData { itemName = car.name, price = price, carPrefab = car });
                EditorUtility.SetDirty(market);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }
}
