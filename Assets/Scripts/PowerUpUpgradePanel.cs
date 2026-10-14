using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Powerups sayfası: her güçlendirmenin süresini coin ile yükseltme satırları.
// Satırların görselleri sahnede durur; bu script yalnızca yazıları, seviye noktalarını ve butonları günceller.
public class PowerUpUpgradePanel : MonoBehaviour
{
    [System.Serializable]
    public class Row
    {
        public UpgradeType type;
        [Tooltip("Yükseltmesiz temel süre (saniye). Oyundaki değerle aynı olmalı; yalnızca ekranda göstermek için.")]
        public float baseSeconds = 5f;
        public TextMeshProUGUI durationText;
        public Image[] levelPips;
        public Button upgradeButton;
        public TextMeshProUGUI priceText;
        public GameObject priceCoinIcon;
    }

    public Row[] rows;
    public TextMeshProUGUI walletText;

    [Header("Onay Penceresi")]
    [Tooltip("Yükseltme butonuna basınca açılan \"emin misin?\" penceresi. Boşsa doğrudan satın alınır.")]
    public GameObject confirmPanel;
    public TextMeshProUGUI confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private Row pendingRow;

    [Header("Renkler")]
    public Color pipOnColor = new Color32(138, 221, 84, 255);
    public Color pipOffColor = new Color32(30, 30, 90, 255);
    public Color buttonReadyColor = new Color32(138, 221, 84, 255);
    public Color buttonNoMoneyColor = new Color32(120, 120, 140, 255);
    public Color buttonMaxColor = new Color32(246, 198, 62, 255);

    void Awake()
    {
        foreach (Row row in rows)
        {
            Row r = row; // closure için kopya
            if (r.upgradeButton != null) r.upgradeButton.onClick.AddListener(() => OnUpgradeClicked(r));
        }
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(ConfirmUpgrade);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(CloseConfirm);
    }

    void OnEnable()
    {
        CloseConfirm();
        Refresh();
    }

    // Butona basınca hemen satın alınmaz; önce onay penceresi açılır
    void OnUpgradeClicked(Row row)
    {
        if (confirmPanel == null)
        {
            Buy(row);
            return;
        }

        int price = PowerUpUpgrades.NextPrice(row.type);
        if (price < 0) return;

        pendingRow = row;
        if (confirmText != null)
        {
            int nextLevel = PowerUpUpgrades.GetLevel(row.type) + 1;
            confirmText.text = $"UPGRADE {PowerUpUpgrades.DisplayName(row.type)} TO LEVEL {nextLevel} FOR {price.Dotted()} COINS?";
        }
        confirmPanel.SetActive(true);
    }

    void ConfirmUpgrade()
    {
        if (pendingRow != null) Buy(pendingRow);
        CloseConfirm();
    }

    void CloseConfirm()
    {
        pendingRow = null;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    void Buy(Row row)
    {
        if (PowerUpUpgrades.TryUpgrade(row.type))
        {
            if (AudioManager.instance != null && AudioManager.instance.coinSound != null)
                AudioManager.instance.PlayUI(AudioManager.instance.coinSound);
        }
        Refresh();
    }

    public void Refresh()
    {
        int gems = PlayerPrefs.GetInt("GemsCount", 0);
        if (walletText != null) walletText.text = gems.Dotted();

        foreach (Row row in rows)
        {
            int level = PowerUpUpgrades.GetLevel(row.type);
            int price = PowerUpUpgrades.NextPrice(row.type);
            float now = row.baseSeconds + PowerUpUpgrades.Bonus(row.type);

            if (row.durationText != null)
            {
                string current = now.ToString("0.0", CultureInfo.InvariantCulture);
                string next = (now + PowerUpUpgrades.StepSeconds(row.type)).ToString("0.0", CultureInfo.InvariantCulture);
                row.durationText.text = price < 0 ? $"{current}s" : $"{current}s  >  {next}s";
            }

            for (int i = 0; i < row.levelPips.Length; i++)
                if (row.levelPips[i] != null) row.levelPips[i].color = i < level ? pipOnColor : pipOffColor;

            bool isMax = price < 0;
            bool canAfford = !isMax && gems >= price;
            if (row.priceText != null) row.priceText.text = isMax ? "MAX" : price.Dotted();
            if (row.priceCoinIcon != null) row.priceCoinIcon.SetActive(!isMax);
            if (row.upgradeButton != null)
            {
                row.upgradeButton.interactable = canAfford;
                var img = row.upgradeButton.targetGraphic as Image;
                if (img != null) img.color = isMax ? buttonMaxColor : canAfford ? buttonReadyColor : buttonNoMoneyColor;
            }
        }
    }
}
