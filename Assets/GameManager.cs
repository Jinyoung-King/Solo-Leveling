using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;

[System.Serializable]
public class Equipment
{
    public string equipName;
    public string rarity; // "Common", "Rare", "Epic", "Legendary"
    public int level = 0; // 중복 획득 시 레벨업
    public float statBonus; // 추가 수치 (LPS 가산 또는 배율)
    public string bonusType; // "LPS", "Multiplier", "Click"
    public string description;

    public Equipment(string name, string rarity, float bonus, string type, string desc)
    {
        equipName = name;
        this.rarity = rarity;
        statBonus = bonus;
        bonusType = type;
        description = desc;
        level = 0;
    }
}

[System.Serializable]
public class Unit
{
    public string unitName;
    public long revenuePerSec;
    public long baseCost;
    public long currentCost;
    public int count = 0;
    public Sprite iconSprite;

    public Unit(string name, int tier)
    {
        unitName = name;
        baseCost = (long)(10 * Mathf.Pow(10, tier));
        revenuePerSec = (long)(1 * Mathf.Pow(5, tier));
        currentCost = baseCost;
    }

    public void SetIcon(Sprite sprite)
    {
        this.iconSprite = sprite;
    }

    public void Buy()
    {
        count++;
        currentCost = (long)(currentCost * 1.5f);
    }

    public void LoadCount(int savedCount)
    {
        count = savedCount;
        currentCost = (long)(baseCost * Mathf.Pow(1.5f, savedCount));
    }
}

public class GameManager : MonoBehaviour
{
    [Header("UI System")]
    public Transform shopContent;
    public GameObject shopButtonPrefab;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI myInfoText;

    [Header("Popup UI")]
    public GameObject rewardPopup;
    public TextMeshProUGUI rewardMessage;

    [Header("Server Upgrade")]
    public TextMeshProUGUI upgradeText;
    public Button upgradeButton;

    [Header("Game Data")]
    public long logs = 0;
    public int serverLevel = 1;
    public long upgradeCost = 100;
    public List<Unit> units = new List<Unit>();
    private List<TextMeshProUGUI> generatedButtonTexts = new List<TextMeshProUGUI>();
    public TerminalManager terminalManager;

    [Header("Resources")]
    public Sprite[] techIcons;

    [Header("Juice Effect")]
    public ObjectShaker buttonShaker;

    string[] techNames = {
        "Shell Script", "Docker", "Kubernetes", "AWS EC2", "Serverless Lambda",
        "Kafka Cluster", "Elasticsearch", "Quantum Server", "AI DevOps Bot", "Dyson Sphere"
    };

    private float timer = 0f;

    [Header("Animation")]
    public Transform serverButtonTr;

    [Header("Effect")]
    public GameObject effectPrefab;
    public Transform effectParent;

    [Header("Skill System")]
    public Button coffeeBtn;
    public Slider coffeeSlider;
    public TextMeshProUGUI boostText;

    [Header("Overclock System")]
    public Slider heatSlider;
    public Image backgroundPanel;

    private Coroutine bounceRoutine;

    [Header("Prestige System")]
    public GameObject migrationPanel;
    public TextMeshProUGUI migrationInfoText;
    public int goldenDisks = 0;

    [Header("Bug Event")]
    public Button bugButton;
    public RectTransform canvasRect;

    [Header("Gacha UI System")]
    public GameObject gachaPanel;
    public TextMeshProUGUI gachaResultText;
    public TextMeshProUGUI equipmentListText;
    public TextMeshProUGUI gachaCostText;

    [HideInInspector]
    public List<Equipment> equipments = new List<Equipment>();
    [HideInInspector]
    public long gachaCost = 50000;

    // Sub-Managers (Runtime dynamic components)
    private SaveManager saveManager;
    private GachaManager gachaManager;
    private SkillManager skillManager;
    private OverclockManager overclockManager;
    private BugManager bugManager;
    private PrestigeManager prestigeManager;

    // Helper properties to access sub-manager states
    public bool IsCoffeeActive => skillManager != null && skillManager.IsCoffeeTime;
    public bool IsOverclockActive => overclockManager != null && overclockManager.IsOverclock;

    void Start()
    {
        Application.targetFrameRate = 60;

        // Attach and initialize sub-managers dynamically to clean concerns
        saveManager = gameObject.AddComponent<SaveManager>();
        gachaManager = gameObject.AddComponent<GachaManager>();
        skillManager = gameObject.AddComponent<SkillManager>();
        overclockManager = gameObject.AddComponent<OverclockManager>();
        bugManager = gameObject.AddComponent<BugManager>();
        prestigeManager = gameObject.AddComponent<PrestigeManager>();

        saveManager.Initialize(this);
        gachaManager.Initialize(this);
        skillManager.Initialize(this);
        overclockManager.Initialize(this);
        bugManager.Initialize(this);
        prestigeManager.Initialize(this);

        GenerateTechStack();
        LoadGame();
        UpdateUI();
    }

    void GenerateTechStack()
    {
        for (int i = 0; i < techNames.Length; i++)
        {
            Unit newUnit = new Unit(techNames[i], i);
            if (i < techIcons.Length)
            {
                newUnit.SetIcon(techIcons[i]);
            }
            units.Add(newUnit);

            GameObject btnObj = Instantiate(shopButtonPrefab, shopContent);
            
            Image iconImage = null;
            Image[] allImages = btnObj.GetComponentsInChildren<Image>(true);
            Button btnComponent = btnObj.GetComponent<Button>();
            foreach (var img in allImages)
            {
                if (btnComponent != null && img == btnComponent.targetGraphic) continue;
                iconImage = img;
                break;
            }

            if (iconImage != null && newUnit.iconSprite != null)
            {
                iconImage.sprite = newUnit.iconSprite;
            }

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            generatedButtonTexts.Add(txt);

            int index = i;
            btnObj.GetComponent<Button>().onClick.AddListener(() => BuyUnit(index));
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            long totalRevenue = GetTotalRevenue();
            logs += totalRevenue;
            timer -= 1f;
            UpdateUI();
        }

        if (Input.GetKeyDown(KeyCode.Delete)) { DeleteAllData(); }
    }

    public void OnClickServer()
    {
        long clickProfit = serverLevel;
        clickProfit += GetEquipmentClick();

        float prestigeMultiplier = 1f + (goldenDisks * 0.1f);
        float equipMultiplier = GetEquipmentMultiplier();
        clickProfit = (long)(clickProfit * prestigeMultiplier * equipMultiplier);

        if (IsOverclockActive) clickProfit *= 5;

        bool isCritical = (UnityEngine.Random.Range(0, 100) < 10);
        if (isCritical)
        {
            long multiplier = 50 + (serverLevel * 2);
            long knowledgeBonus = 0;
            foreach (var u in units) knowledgeBonus += u.count;
            knowledgeBonus *= 10;

            clickProfit = (clickProfit * multiplier) + knowledgeBonus;

            float chaosFactor = UnityEngine.Random.Range(0.8f, 1.2f);
            clickProfit = (long)(clickProfit * chaosFactor);

            if (buttonShaker != null) buttonShaker.Shake();
            terminalManager?.AddLog("<color=red>[FATAL] CRITICAL PACKET!</color>");
        }
        else
        {
            if (UnityEngine.Random.Range(0, 10) < 2) terminalManager?.AddLog("<color=yellow>Packet received.</color>");
        }

        if (overclockManager != null)
        {
            overclockManager.AddHeat();
        }

        logs += clickProfit;
        UpdateUI();

        if (bounceRoutine != null) StopCoroutine(bounceRoutine);
        bounceRoutine = StartCoroutine(BounceAnimation());
        ShowClickEffect(clickProfit, isCritical);
    }

    public long GetTotalRevenue()
    {
        long total = 0;
        foreach (Unit unit in units) total += (unit.count * unit.revenuePerSec);

        total += GetEquipmentLPS();

        if (IsCoffeeActive) total *= 2;
        if (IsOverclockActive) total *= 5;

        float prestigeMultiplier = 1f + (goldenDisks * 0.1f);
        float equipMultiplier = GetEquipmentMultiplier();
        total = (long)(total * prestigeMultiplier * equipMultiplier);

        return total;
    }

    public float GetTotalMultiplier()
    {
        float totalMultiplier = 1f;
        if (IsCoffeeActive) totalMultiplier *= 2f;
        if (IsOverclockActive) totalMultiplier *= 5f;
        totalMultiplier *= (1f + (goldenDisks * 0.1f));
        totalMultiplier *= GetEquipmentMultiplier();
        return totalMultiplier;
    }

    public void BuyUnit(int index)
    {
        Unit target = units[index];
        if (logs >= target.currentCost)
        {
            logs -= target.currentCost;
            target.Buy();
            UpdateUI();
            SaveGame();
            terminalManager?.AddLog($"<color=yellow><b>Deployed: {target.unitName}</b></color>");
        }
    }

    public void UpgradeServer()
    {
        if (logs >= upgradeCost)
        {
            logs -= upgradeCost;
            serverLevel++;
            upgradeCost = (long)(upgradeCost * 2.5f);
            UpdateUI();
            SaveGame();
            terminalManager?.AddLog($"<color=blue><b>SERVER UPGRADE! Lv.{serverLevel}</b></color>");
        }
    }

    public string FormatNumber(long num)
    {
        if (num >= 1000000000000000) return (num / 1000000000000000f).ToString("F2") + "P";
        if (num >= 1000000000000) return (num / 1000000000000f).ToString("F2") + "T";
        if (num >= 1000000000) return (num / 1000000000f).ToString("F2") + "G";
        if (num >= 1000000) return (num / 1000000f).ToString("F2") + "M";
        if (num >= 1000) return (num / 1000f).ToString("F2") + "K";
        return num.ToString("N0");
    }

    public void SaveGame() => saveManager?.SaveGame();
    public void LoadGame() => saveManager?.LoadGame();
    public void OnClickBug() => bugManager?.OnClickBug();

    public void ShowRewardPopup(double seconds, long reward)
    {
        if (rewardPopup != null && rewardMessage != null)
        {
            rewardMessage.text = $"[배치 작업 리포트]\n\n지난 {seconds:F0}초 동안\n서버가 열심히 돌아서\n\n<color=yellow>+{FormatNumber(reward)} Logs</color>\n를 수집했습니다!";
            rewardPopup.SetActive(true);
            rewardPopup.transform.SetAsLastSibling();
        }
    }

    public void CloseRewardPopup()
    {
        if (rewardPopup != null) rewardPopup.SetActive(false);
    }

    void ShowClickEffect(long profit, bool isCritical)
    {
        GameObject instance = Instantiate(effectPrefab, Input.mousePosition, Quaternion.identity, effectParent);
        FloatingText floatingText = instance.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            if (isCritical) floatingText.SetCritical($"CRITICAL!\n+{FormatNumber(profit)}");
            else floatingText.SetText($"+{FormatNumber(profit)}");
        }
    }

    IEnumerator BounceAnimation()
    {
        Vector3 startScale = Vector3.one;
        Vector3 targetScale = Vector3.one * 0.9f;

        float time = 0f;
        while (time < 0.1f)
        {
            time += Time.deltaTime;
            serverButtonTr.localScale = Vector3.Lerp(startScale, targetScale, time / 0.1f);
            yield return null;
        }

        time = 0f;
        while (time < 0.1f)
        {
            time += Time.deltaTime;
            serverButtonTr.localScale = Vector3.Lerp(targetScale, startScale, time / 0.1f);
            yield return null;
        }
        serverButtonTr.localScale = startScale;
    }

    public void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{FormatNumber(logs)} <size=70%>Logs</size>";
            scoreText.color = IsCoffeeActive ? Color.yellow : Color.green;
        }

        if (myInfoText != null)
        {
            long currentLPS = GetTotalRevenue();
            float totalMultiplier = GetTotalMultiplier();
            myInfoText.text = 
                $"<color=#00FF00>⚡ {FormatNumber(currentLPS)} /sec</color>\n" +
                $"<color=#FFD700>💾 Disk: {goldenDisks}</color> | <color=#00FFFF>💻 Lv.{serverLevel}</color>\n" +
                $"<size=80%>(Current Boost: <color=orange>x{totalMultiplier:F1}</color>)</size>";
        }

        if (upgradeText != null)
        {
            upgradeText.text = $"<b>CPU Upgrade (Lv.{serverLevel})</b>\nCost: {FormatNumber(upgradeCost)}";
            upgradeText.color = (logs >= upgradeCost) ? Color.cyan : Color.red;
        }

        if (generatedButtonTexts != null && units != null)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (i < generatedButtonTexts.Count && generatedButtonTexts[i] != null)
                {
                    Unit u = units[i];
                    string colorHex = (logs >= u.currentCost) ? "#00FF00" : "#FF0000";
                    generatedButtonTexts[i].text =
                        $"<b>{u.unitName}</b> <color=orange>Lv.{u.count}</color>\n" +
                        $"<size=80%>{FormatNumber(u.revenuePerSec)} /sec</size>\n" +
                        $"<color={colorHex}>Cost: {FormatNumber(u.currentCost)}</color>";
                }
            }
        }
    }

    public void ActivateCoffee() => skillManager?.ActivateCoffee();
    public void OpenMigrationPopup() => prestigeManager?.OpenMigrationPopup();
    public void CloseMigrationPopup() => prestigeManager?.CloseMigrationPopup();
    public void ConfirmMigration() => prestigeManager?.ConfirmMigration();

    // Gacha delegations
    public long GetEquipmentLPS() => gachaManager != null ? gachaManager.GetEquipmentLPS() : 0;
    public long GetEquipmentClick() => gachaManager != null ? gachaManager.GetEquipmentClick() : 0;
    public float GetEquipmentMultiplier() => gachaManager != null ? gachaManager.GetEquipmentMultiplier() : 1f;
    public void OpenGachaPanel() => gachaManager?.OpenGachaPanel();
    public void CloseGachaPanel() => gachaManager?.CloseGachaPanel();
    public void DrawEquipmentGacha() => gachaManager?.DrawEquipmentGacha();
    public void UpdateGachaUI() => gachaManager?.UpdateGachaUI();

    void DeleteAllData()
    {
        PlayerPrefs.DeleteAll();
        logs = 0;
        serverLevel = 1;
        upgradeCost = 100;
        goldenDisks = 0;
        gachaCost = 50000;
        foreach (var u in units) { u.count = 0; u.LoadCount(0); }
        if (gachaManager != null) gachaManager.InitializeEquipments();
        UpdateUI();
        terminalManager?.AddLog("[INFO] <color=red><b>SYSTEM RESET COMPLETED.</b></color>");
    }
}