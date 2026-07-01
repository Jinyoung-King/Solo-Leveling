using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GachaManager : MonoBehaviour
{
    private GameManager gameManager;

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        if (gameManager.equipments == null || gameManager.equipments.Count == 0)
        {
            InitializeEquipments();
        }
    }

    public void InitializeEquipments()
    {
        if (gameManager == null) return;
        if (gameManager.equipments == null) gameManager.equipments = new List<Equipment>();
        gameManager.equipments.Clear();

        // Common (일반)
        gameManager.equipments.Add(new Equipment("E급 헌터 단검", "Common", 2f, "Click", "클릭당 마나 획득 +2"));
        gameManager.equipments.Add(new Equipment("낡은 강철 장검", "Common", 15f, "LPS", "초당 마나 생산 +15"));
        gameManager.equipments.Add(new Equipment("수습 마법사의 지팡이", "Common", 40f, "LPS", "초당 마나 생산 +40"));

        // Rare (희귀)
        gameManager.equipments.Add(new Equipment("D급 헌터의 장검", "Rare", 10f, "Click", "클릭당 마나 획득 +10"));
        gameManager.equipments.Add(new Equipment("기사의 붉은 방패", "Rare", 200f, "LPS", "초당 마나 생산 +200"));
        gameManager.equipments.Add(new Equipment("정예 저격수의 활", "Rare", 500f, "LPS", "초당 마나 생산 +500"));

        // Epic (에픽)
        gameManager.equipments.Add(new Equipment("B급 암살자의 비수", "Epic", 50f, "Click", "클릭당 마나 획득 +50"));
        gameManager.equipments.Add(new Equipment("하이오크 주술 보주", "Epic", 2000f, "LPS", "초당 마나 생산 +2,000"));
        gameManager.equipments.Add(new Equipment("성기사의 영광된 갑옷", "Epic", 6000f, "LPS", "초당 마나 생산 +6,000"));

        // Legendary (전설)
        gameManager.equipments.Add(new Equipment("카사카의 독니", "Legendary", 0.5f, "Multiplier", "마력 오버로드/기본 배율 +50% (+0.5)"));
        gameManager.equipments.Add(new Equipment("악마왕의 단검", "Legendary", 1.0f, "Multiplier", "그림자 영토 전체 마나 수입 +100% (+1.0)"));
    }

    public long GetEquipmentLPS()
    {
        long lps = 0;
        if (gameManager == null || gameManager.equipments == null) return 0;
        foreach (var eq in gameManager.equipments)
        {
            if (eq.bonusType == "LPS") lps += (long)(eq.statBonus * eq.level);
        }
        return lps;
    }

    public long GetEquipmentClick()
    {
        long click = 0;
        if (gameManager == null || gameManager.equipments == null) return 0;
        foreach (var eq in gameManager.equipments)
        {
            if (eq.bonusType == "Click") click += (long)(eq.statBonus * eq.level);
        }
        return click;
    }

    public float GetEquipmentMultiplier()
    {
        float mult = 1f;
        if (gameManager == null || gameManager.equipments == null) return 1f;
        foreach (var eq in gameManager.equipments)
        {
            if (eq.bonusType == "Multiplier") mult += (eq.statBonus * eq.level);
        }
        return mult;
    }

    public void OpenGachaPanel()
    {
        if (gameManager == null) return;
        if (gameManager.gachaPanel != null)
        {
            gameManager.gachaPanel.SetActive(true);
            gameManager.gachaPanel.transform.SetAsLastSibling();
            UpdateGachaUI();
        }
    }

    public void CloseGachaPanel()
    {
        if (gameManager == null) return;
        if (gameManager.gachaPanel != null) gameManager.gachaPanel.SetActive(false);
    }

    public void DrawEquipmentGacha()
    {
        if (gameManager == null) return;
        if (gameManager.equipments == null || gameManager.equipments.Count == 0) InitializeEquipments();

        if (gameManager.logs < gameManager.gachaCost)
        {
            string errStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("insufficient_mana_gacha") : "[ERROR] 가차 비용이 부족합니다!";
            gameManager.terminalManager?.AddLog($"<color=red>{errStr}</color>");
            return;
        }

        gameManager.logs -= gameManager.gachaCost;
        long lastCost = gameManager.gachaCost;
        gameManager.gachaCost = (long)(gameManager.gachaCost * 1.25f); // 비용 점진적 25% 상승

        // 가차 뽑기 (확률: Common 60%, Rare 30%, Epic 8.5%, Legendary 1.5%)
        float dice = UnityEngine.Random.Range(0f, 100f);
        string targetRarity = "Common";

        if (dice < 1.5f) targetRarity = "Legendary";
        else if (dice < 10.0f) targetRarity = "Epic";
        else if (dice < 40.0f) targetRarity = "Rare";
        else targetRarity = "Common";

        List<Equipment> candidates = gameManager.equipments.FindAll(e => e.rarity == targetRarity);
        if (candidates.Count == 0) candidates = gameManager.equipments;

        Equipment drawn = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        drawn.level++;

        gameManager.SaveGame();
        gameManager.UpdateUI();
        UpdateGachaUI();

        string rarityColor = "#FFFFFF";
        if (drawn.rarity == "Rare") rarityColor = "#32CD32";
        else if (drawn.rarity == "Epic") rarityColor = "#1E90FF";
        else if (drawn.rarity == "Legendary") rarityColor = "#FFD700";

        // 로컬라이즈 이름/효과 가져오기
        string localName = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetEquipmentName(drawn.equipName) : drawn.equipName;
        string localDesc = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetEquipmentDesc(drawn.equipName, drawn.bonusType, drawn.statBonus) : drawn.description;

        // 터미널 피드백
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == Language.English)
        {
            gameManager.terminalManager?.AddLog($"★ <color=yellow><b>[GACHA]</b></color> Obtained <color={rarityColor}><b>[{drawn.rarity}] {localName}</b></color>! (Lv.{drawn.level})");
            gameManager.terminalManager?.AddLog($"   └ Effect: {localDesc}");
        }
        else
        {
            gameManager.terminalManager?.AddLog($"★ <color=yellow><b>[GACHA]</b></color> <color={rarityColor}><b>[{drawn.rarity}] {localName}</b></color> 획득! (현재 Lv.{drawn.level})");
            gameManager.terminalManager?.AddLog($"   └ 효과: {localDesc}");
        }

        if (gameManager.gachaResultText != null)
        {
            string drawTitle = LocalizationManager.Instance != null ? (LocalizationManager.Instance.CurrentLanguage == Language.English ? "Obtained!" : "획득!") : "획득!";
            gameManager.gachaResultText.text = $"★ <color={rarityColor}><b>[{drawn.rarity}]</b></color> {drawTitle}\n<size=120%><b>{localName}</b></size>\n\n<size=80%>{localDesc}</size>";
        }

        if (gameManager.buttonShaker != null && drawn.rarity == "Legendary")
        {
            gameManager.buttonShaker.Shake();
        }
    }

    public void UpdateGachaUI()
    {
        if (gameManager == null) return;

        string suffix = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("mana") : "Mana";
        string drawBtnLabel = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("gacha_draw_btn") : "★ 1회 뽑기";

        if (gameManager.gachaCostText != null)
        {
            gameManager.gachaCostText.text = $"{drawBtnLabel}\nCost: {gameManager.FormatNumber(gameManager.gachaCost)} {suffix}";
            gameManager.gachaCostText.color = (gameManager.logs >= gameManager.gachaCost) ? Color.yellow : Color.red;
        }

        if (gameManager.equipmentListText != null && gameManager.equipments != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string bookTitle = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("gacha_book_title") : "=== 보유 장비 목록 (도감) ===";
            sb.AppendLine(bookTitle);
            foreach (var eq in gameManager.equipments)
            {
                string rarityColor = "#FFFFFF";
                if (eq.rarity == "Rare") rarityColor = "#32CD32";
                else if (eq.rarity == "Epic") rarityColor = "#1E90FF";
                else if (eq.rarity == "Legendary") rarityColor = "#FFD700";

                string eqName = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetEquipmentName(eq.equipName) : eq.equipName;
                string eqDesc = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetEquipmentDesc(eq.equipName, eq.bonusType, eq.statBonus) : eq.description;

                string lvPrefix = LocalizationManager.Instance != null ? (LocalizationManager.Instance.CurrentLanguage == Language.English ? "Lv." : "Lv.") : "Lv.";
                string sumLabel = LocalizationManager.Instance != null ? (LocalizationManager.Instance.CurrentLanguage == Language.English ? "Total: +" : "합계: +") : "합계: +";

                sb.AppendLine($"<color={rarityColor}>[{eq.rarity}]</color> {eqName} <color=orange>{lvPrefix}{eq.level}</color>");
                sb.AppendLine($"<size=85%>{eqDesc} ({sumLabel}{eq.statBonus * eq.level:F1})</size>");
            }
            gameManager.equipmentListText.text = sb.ToString();
        }
    }
}
