using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;



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
    public int currentGate = 1; // 게이트 진행도

    [Header("Gate System")]
    [HideInInspector] public GameObject gateBossPanel;
    [HideInInspector] public GameObject skillBarPanel;
    public List<Unit> units = new List<Unit>();
    private List<TextMeshProUGUI> generatedButtonTexts = new List<TextMeshProUGUI>();
    public TerminalManager terminalManager;

    [Header("Resources")]
    public Sprite[] techIcons;

    [Header("Juice Effect")]
    public ObjectShaker buttonShaker;

    string[] techNames = {
        "그림자 보병 (Infantry)",
        "그림자 궁수 (Archer)",
        "그림자 순찰병 (Scout)",
        "그림자 마법사 (Mage)",
        "그림자 암살자 (Assassin)",
        "그림자 거인 (Giant)",
        "그림자 와이번 (Wyvern)",
        "하이오크 주술사 (High Orc)",
        "지휘관 이그리트 (Igris)",
        "그림자 장군 베르 (Beru)"
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
    private GateManager gateManager;
    private SkillBarManager skillBarManager;
    private MissionManager missionManager;

    // Helper properties to access sub-manager states
    public bool IsCoffeeActive => skillManager != null && skillManager.IsCoffeeTime;
    public bool IsOverclockActive => overclockManager != null && overclockManager.IsOverclock;

    void Start()
    {
        Application.targetFrameRate = 60;

        if (FindObjectOfType<LocalizationManager>() == null)
        {
            gameObject.AddComponent<LocalizationManager>();
        }

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

        // 게이트 진행 매니저 (LoadGame 이후 currentGate 로 초기화)
        gateManager = gameObject.AddComponent<GateManager>();
        gateManager.Initialize(this);

        // 군주 액티브 스킬바
        skillBarManager = gameObject.AddComponent<SkillBarManager>();
        skillBarManager.Initialize(this);

        // 일일 임무 / 연속 출석
        missionManager = gameObject.AddComponent<MissionManager>();
        missionManager.Initialize(this);

        UpdateUI();
        CreateLanguageButton();

        // 탭 매니저 동적 생성 및 연동
        TabManager tabManager = gameObject.AddComponent<TabManager>();
        tabManager.Initialize(this);
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
            gateManager?.DealDamage(totalRevenue); // 초당생산 = 게이트 보스 지속 딜
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
        clickProfit = (long)(clickProfit * prestigeMultiplier * equipMultiplier * GetGateMultiplier());

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
            string critStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("crit_shadow_damage") : "[CRITICAL] SHADOW DAMAGE!";
            terminalManager?.AddLog($"<color=red><b>{critStr}</b></color>");
        }
        else
        {
            if (UnityEngine.Random.Range(0, 10) < 2)
            {
                string absorbStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("crit_mana_absorbed") : "Mana absorbed.";
                terminalManager?.AddLog($"<color=yellow>{absorbStr}</color>");
            }
        }

        if (overclockManager != null)
        {
            overclockManager.AddHeat();
        }

        logs += clickProfit;
        gateManager?.DealDamage(clickProfit); // 클릭 = 게이트 보스 클릭 데미지
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
        total = (long)(total * prestigeMultiplier * equipMultiplier * GetGateMultiplier());

        return total;
    }

    public float GetTotalMultiplier()
    {
        float totalMultiplier = 1f;
        if (IsCoffeeActive) totalMultiplier *= 2f;
        if (IsOverclockActive) totalMultiplier *= 5f;
        totalMultiplier *= (1f + (goldenDisks * 0.1f));
        totalMultiplier *= GetEquipmentMultiplier();
        totalMultiplier *= GetGateMultiplier();
        return totalMultiplier;
    }

    public void BuyUnit(int index)
    {
        Unit target = units[index];
        if (logs >= target.currentCost)
        {
            logs -= target.currentCost;
            target.Buy();
            ReportSummon();
            UpdateUI();
            SaveGame();
            
            string sumPrefix = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("summoned_log") : "Summoned: ";
            string uName = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetUnitName(index) : target.unitName;
            terminalManager?.AddLog($"<color=yellow><b>{sumPrefix}{uName}</b></color>");
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
            
            string lvUpStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("monarch_level_up_log") : "MONARCH LEVEL UP! Lv.";
            terminalManager?.AddLog($"<color=blue><b>{lvUpStr}{serverLevel}</b></color>");
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
            string title = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("offline_popup_title") : "[그림자 영토 탐색 리포트]";
            string descFormat = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("offline_popup_desc") : "지난 {0:F0}초 동안\n그림자 군대가 게이트를 돌아서\n\n<color=yellow>+{1} Mana</color>\n를 수집했습니다!";
            
            rewardMessage.text = $"{title}\n\n" + string.Format(descFormat, seconds, FormatNumber(reward));
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
        string manaLabel = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("mana") : "Mana";

        if (scoreText != null)
        {
            scoreText.text = $"{FormatNumber(logs)} <size=70%>{manaLabel}</size>";
            scoreText.color = IsCoffeeActive ? Color.yellow : Color.green;
        }

        if (myInfoText != null)
        {
            long currentLPS = GetTotalRevenue();
            float totalMultiplier = GetTotalMultiplier();
            
            string marksLabel = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("marks_label") : "Disk";
            string boostLabel = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("boost_label") : "Current Boost";

            myInfoText.text =
                $"<color=#00FF00>{FormatNumber(currentLPS)} /sec</color>\n" +
                $"<color=#FFD700>{marksLabel}: {goldenDisks}</color> | <color=#00FFFF>Lv.{serverLevel}</color>\n" +
                $"<size=80%>({boostLabel}: <color=orange>x{totalMultiplier:F1}</color>)</size>";
        }

        if (upgradeText != null)
        {
            string upLabel = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("monarch_upgrade") : "Monarch Upgrade";
            upgradeText.text = $"<b>{upLabel} (Lv.{serverLevel})</b>\nCost: {FormatNumber(upgradeCost)}";
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
                    string uName = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetUnitName(i) : u.unitName;
                    
                    generatedButtonTexts[i].text =
                        $"<b>{uName}</b> <color=orange>Lv.{u.count}</color>\n" +
                        $"<size=80%>{FormatNumber(u.revenuePerSec)} /sec</size>\n" +
                        $"<color={colorHex}>Cost: {FormatNumber(u.currentCost)}</color>";
                }
            }
        }
    }

    public float GetGateMultiplier() => gateManager != null ? gateManager.GetGateMultiplier() : 1f;
    public void DealGateDamage(long amount) => gateManager?.DealDamage(amount);

    // 임무 진행 이벤트 훅
    public void ReportSummon() => missionManager?.AddProgress(MissionType.Summon);
    public void ReportGateClear() => missionManager?.AddProgress(MissionType.Gate);
    public void ReportBeastPurge() => missionManager?.AddProgress(MissionType.Beast);

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
        currentGate = 1;
        foreach (var u in units) { u.count = 0; u.LoadCount(0); }
        if (gachaManager != null) gachaManager.InitializeEquipments();
        gateManager?.ResetToCurrent();
        UpdateUI();
        terminalManager?.AddLog("[INFO] <color=red><b>SYSTEM RESET COMPLETED.</b></color>");
    }

    void CreateLanguageButton()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Check if button already exists
        Button existing = null;
        Button[] btns = canvas.GetComponentsInChildren<Button>(true);
        foreach (var b in btns)
        {
            if (b.gameObject.name == "Btn_Language")
            {
                existing = b;
                break;
            }
        }
        if (existing != null) return;

        GameObject langBtnGo = new GameObject("Btn_Language", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        langBtnGo.transform.SetParent(canvas.transform, false);

        RectTransform rect = langBtnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20); // 우측 상단 배치
        rect.sizeDelta = new Vector2(120, 60);

        Image img = langBtnGo.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(langBtnGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI txt = textGo.GetComponent<TextMeshProUGUI>();
        txt.fontSize = 16;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        
        // 폰트 상속
        if (scoreText != null) txt.font = scoreText.font;

        Button btn = langBtnGo.GetComponent<Button>();
        btn.onClick.AddListener(() => {
            if (LocalizationManager.Instance != null)
            {
                Language next = LocalizationManager.Instance.CurrentLanguage == Language.Korean ? Language.English : Language.Korean;
                LocalizationManager.Instance.SetLanguage(next);
                txt.text = LocalizationManager.Instance.CurrentLanguage == Language.Korean ? "KR / EN" : "EN / KR";
            }
        });

        txt.text = (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == Language.Korean) ? "KR / EN" : "EN / KR";
    }
}