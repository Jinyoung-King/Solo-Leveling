using UnityEngine;
using System.Collections.Generic;

public enum Language
{
    Korean,
    English
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public Language CurrentLanguage { get; private set; } = Language.Korean;

    private Dictionary<string, Dictionary<Language, string>> localizedStrings = new Dictionary<string, Dictionary<Language, string>>()
    {
        // UI Labels
        { "mana", new Dictionary<Language, string>() { { Language.Korean, "마력" }, { Language.English, "Mana" } } },
        { "monarch_upgrade", new Dictionary<Language, string>() { { Language.Korean, "군주 권능 강화" }, { Language.English, "Monarch Upgrade" } } },
        { "gacha_title", new Dictionary<Language, string>() { { Language.Korean, "★ 헌터 장비 가차 ★" }, { Language.English, "★ HUNTER EQUIPMENT GACHA ★" } } },
        { "gacha_desc", new Dictionary<Language, string>() { { Language.Korean, "마나를 소모하여 헌터 무기 및 장비를 뽑으세요!\n\n<size=80%>(1회 50.0K 마나 -> 뽑을 때마다 상승)</size>" }, { Language.English, "Spend Mana to draw Hunter weapons and gears!\n\n<size=80%>(1회 50.0K Mana -> Increases each draw)</size>" } } },
        { "gacha_draw_btn", new Dictionary<Language, string>() { { Language.Korean, "★ 1회 뽑기" }, { Language.English, "★ 1 Draw" } } },
        { "gacha_close_btn", new Dictionary<Language, string>() { { Language.Korean, "닫기" }, { Language.English, "Close" } } },
        { "gacha_empty_list", new Dictionary<Language, string>() { { Language.Korean, "보유 장비 없음" }, { Language.English, "No equipment owned" } } },
        { "gacha_book_title", new Dictionary<Language, string>() { { Language.Korean, "=== 보유 장비 목록 (도감) ===" }, { Language.English, "=== Equipment Inventory (Index) ===" } } },
        { "potion_cooldown", new Dictionary<Language, string>() { { Language.Korean, "[ERROR] 포션이 제조 중입니다 (재사용 대기)." }, { Language.English, "[ERROR] Potion is still brewing (on cooldown)." } } },
        { "potion_ready", new Dictionary<Language, string>() { { Language.Korean, "포션 제조가 완료되었습니다!" }, { Language.English, "Potion is ready!" } } },
        { "potion_active", new Dictionary<Language, string>() { { Language.Korean, "[BOOST] 각성 포션 복용! 속도 x2" }, { Language.English, "[BOOST] Potion consumed! Speed x2" } } },
        { "potion_worn_off", new Dictionary<Language, string>() { { Language.Korean, "[INFO] 각성 효과가 끝났습니다..." }, { Language.English, "[INFO] Awakening effect worn off..." } } },
        { "mana_overload", new Dictionary<Language, string>() { { Language.Korean, " 경고: 마력 오버로드! (공격력 5배 증가) " }, { Language.English, " WARNING: MANA OVERLOAD! (x5 Boost) " } } },
        { "mana_stabilized", new Dictionary<Language, string>() { { Language.Korean, "마력이 안정되었습니다. 진정 중..." }, { Language.English, "Mana stabilized. Cooling down..." } } },
        { "beast_detected", new Dictionary<Language, string>() { { Language.Korean, "[경고] 차원에 틈새 마수 출현! 처단하세요!" }, { Language.English, "[ALERT] Magical Beast detected! Purge it!" } } },
        { "beast_escaped", new Dictionary<Language, string>() { { Language.Korean, "마수가 게이트 속으로 도망쳤습니다..." }, { Language.English, "Beast escaped into the gate..." } } },
        { "beast_fixed", new Dictionary<Language, string>() { { Language.Korean, "마수 퇴치 완료! 보상: +" }, { Language.English, "BEAST PURGED! Reward: +" } } },
        { "no_beasts", new Dictionary<Language, string>() { { Language.Korean, "게이트 내에 활성화된 마수가 없습니다." }, { Language.English, "No active magical beasts detected in the gate." } } },
        { "insufficient_mana_upgrade", new Dictionary<Language, string>() { { Language.Korean, "[ERROR] 마력이 부족하여 군주 권능을 강화할 수 없습니다." }, { Language.English, "[ERROR] Insufficient Mana to upgrade Monarch Authority." } } },
        { "insufficient_mana_gacha", new Dictionary<Language, string>() { { Language.Korean, "[ERROR] 가차 비용이 부족합니다!" }, { Language.English, "[ERROR] Insufficient Mana for gacha!" } } },
        { "insufficient_mana_summon", new Dictionary<Language, string>() { { Language.Korean, "[ERROR] 마력이 부족하여 소환할 수 없습니다. 필요 비용: " }, { Language.English, "[ERROR] Insufficient mana to summon. Cost: " } } },
        { "summoned_log", new Dictionary<Language, string>() { { Language.Korean, "소환 완료: " }, { Language.English, "Summoned: " } } },
        { "monarch_level_up_log", new Dictionary<Language, string>() { { Language.Korean, "군주 권능 강화! Lv." }, { Language.English, "MONARCH LEVEL UP! Lv." } } },
        { "help_top", new Dictionary<Language, string>() { { Language.Korean, "=== 사용 가능한 권능 명령어 ===" }, { Language.English, "=== AVAILABLE COMMANDS ===" } } },
        { "help_status", new Dictionary<Language, string>() { { Language.Korean, "Display 군주 상태 및 마력 통계 정보" }, { Language.English, "Display monarch status & mana stats" } } },
        { "help_ps", new Dictionary<Language, string>() { { Language.Korean, "List 활성화된 그림자 군대 유닛 목록" }, { Language.English, "List active shadow army (units)" } } },
        { "help_ping", new Dictionary<Language, string>() { { Language.Korean, "Probe 게이트 포탈 마력 연결 지연 테스트" }, { Language.English, "Probe gate connection latency" } } },
        { "help_upgrade", new Dictionary<Language, string>() { { Language.Korean, "Upgrade 군주 권능 레벨업 (클릭과 동일)" }, { Language.English, "Upgrade Monarch Authority (same as Monarch Upgrade)" } } },
        { "help_coffee", new Dictionary<Language, string>() { { Language.Korean, "Drink 각성 포션 복용 버프 발동" }, { Language.English, "Drink caffeine potion booster (if available)" } } },
        { "help_bugfix", new Dictionary<Language, string>() { { Language.Korean, "Purge 출현한 차원의 마수 처단" }, { Language.English, "Purge active magical beast event" } } },
        { "help_buy", new Dictionary<Language, string>() { { Language.Korean, "Summon 그림자 군사 소환 (예: 'buy 보병')" }, { Language.English, "Summon a shadow soldier (e.g. 'buy infantry')" } } },
        { "help_gacha", new Dictionary<Language, string>() { { Language.Korean, "Draw 무작위 헌터 무기/장비 뽑기" }, { Language.English, "Draw random hunter equipment (Costs 50.0K+ Mana)" } } },
        { "cli_initial", new Dictionary<Language, string>() { { Language.Korean, "차원 게이트 연결이 완료되었습니다. 환영합니다, 그림자 군주님!" }, { Language.English, "Gate connection initialized. Welcome, Shadow Monarch!" } } },
        { "cli_help_info", new Dictionary<Language, string>() { { Language.Korean, "사용 가능한 명령어를 보려면 'help'를 입력하세요." }, { Language.English, "Type 'help' to see available commands." } } },
        { "monarch_status_title", new Dictionary<Language, string>() { { Language.Korean, "=== 그림자 군주 상태 ===" }, { Language.English, "=== MONARCH STATUS ===" } } },
        { "monarch_level_label", new Dictionary<Language, string>() { { Language.Korean, "군주 레벨" }, { Language.English, "Monarch Level" } } },
        { "monarch_mana_label", new Dictionary<Language, string>() { { Language.Korean, "보유 마력" }, { Language.English, "Current Mana" } } },
        { "monarch_ps_label", new Dictionary<Language, string>() { { Language.Korean, "유닛 상세를 보려면 'ps'를 입력하세요." }, { Language.English, "Type 'ps' to check shadow army details." } } },
        { "active_army_title", new Dictionary<Language, string>() { { Language.Korean, "=== 소환된 그림자 군대 ===" }, { Language.English, "=== ACTIVE SHADOW ARMY ===" } } },
        { "no_active_army", new Dictionary<Language, string>() { { Language.Korean, "소환된 군사가 없습니다. 군사를 소환해 마력을 수집하세요." }, { Language.English, "No active shadow soldiers. Summon shadows to start harvesting mana." } } },
        { "shadow_not_found", new Dictionary<Language, string>() { { Language.Korean, "[ERROR] 해당하는 그림자 군사 타입을 찾을 수 없습니다: " }, { Language.English, "[ERROR] Shadow type matching '{0}' not found." } } },
        { "offline_popup_title", new Dictionary<Language, string>() { { Language.Korean, "[그림자 영토 탐색 리포트]" }, { Language.English, "[Shadow Territory Report]" } } },
        { "offline_popup_desc", new Dictionary<Language, string>() { { Language.Korean, "지난 {0:F0}초 동안\n그림자 군대가 게이트를 돌아서\n\n<color=yellow>+{1} 마력</color>\n을 수집했습니다!" }, { Language.English, "During the last {0:F0}s,\nyour shadow army explored the gate and gathered:\n\n<color=yellow>+{1} Mana</color>!" } } },
        { "prestige_popup_desc", new Dictionary<Language, string>() { { Language.Korean, "그림자 영토를 다음 차원 게이트로 이전하겠습니까?\n\n현재 그림자 군사들은 <color=red>봉인(초기화)</color>되지만\n<color=yellow>어둠의 징표 {0}개</color>를 획득합니다.\n\n현재 보유: {1}개\n총 마력 보너스: <color=green>+{2}%</color>" }, { Language.English, "Migrate shadow territory to the next gate?\n\nYour shadow army will be <color=red>sealed (reset)</color>,\nbut you will receive <color=yellow>{0} Dark Marks</color>.\n\nCurrently Owned: {1}\nTotal Mana Multiplier: <color=green>+{2}%</color>" } } },
        { "prestige_completed_log", new Dictionary<Language, string>() { { Language.Korean, "=== 그림자 이전 완료 ===" }, { Language.English, "=== SHADOW MIGRATION COMPLETED ===" } } },
        { "prestige_bonus_log", new Dictionary<Language, string>() { { Language.Korean, "새로운 각성 보너스: 전체 마력 수입 +" }, { Language.English, "New Bonus: +{0}% Mana Power!" } } },
        { "marks_label", new Dictionary<Language, string>() { { Language.Korean, "징표" }, { Language.English, "Mark" } } },
        { "boost_label", new Dictionary<Language, string>() { { Language.Korean, "버프 배율" }, { Language.English, "Current Boost" } } },
        { "crit_shadow_damage", new Dictionary<Language, string>() { { Language.Korean, "[CRITICAL] 그림자 폭발 피해!" }, { Language.English, "[CRITICAL] SHADOW DAMAGE!" } } },
        { "crit_mana_absorbed", new Dictionary<Language, string>() { { Language.Korean, "마력 흡수 완료." }, { Language.English, "Mana absorbed." } } }
    };

    private Dictionary<int, Dictionary<Language, string>> unitNames = new Dictionary<int, Dictionary<Language, string>>()
    {
        { 0, new Dictionary<Language, string>() { { Language.Korean, "그림자 보병 (Infantry)" }, { Language.English, "Shadow Infantry" } } },
        { 1, new Dictionary<Language, string>() { { Language.Korean, "그림자 궁수 (Archer)" }, { Language.English, "Shadow Archer" } } },
        { 2, new Dictionary<Language, string>() { { Language.Korean, "그림자 순찰병 (Scout)" }, { Language.English, "Shadow Scout" } } },
        { 3, new Dictionary<Language, string>() { { Language.Korean, "그림자 마법사 (Mage)" }, { Language.English, "Shadow Mage" } } },
        { 4, new Dictionary<Language, string>() { { Language.Korean, "그림자 암살자 (Assassin)" }, { Language.English, "Shadow Assassin" } } },
        { 5, new Dictionary<Language, string>() { { Language.Korean, "그림자 거인 (Giant)" }, { Language.English, "Shadow Giant" } } },
        { 6, new Dictionary<Language, string>() { { Language.Korean, "그림자 와이번 (Wyvern)" }, { Language.English, "Shadow Wyvern" } } },
        { 7, new Dictionary<Language, string>() { { Language.Korean, "하이오크 주술사 (High Orc)" }, { Language.English, "High Orc Shaman" } } },
        { 8, new Dictionary<Language, string>() { { Language.Korean, "지휘관 이그리트 (Igris)" }, { Language.English, "Commander Igris" } } },
        { 9, new Dictionary<Language, string>() { { Language.Korean, "그림자 장군 베르 (Beru)" }, { Language.English, "General Beru" } } }
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLanguage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadLanguage()
    {
        int langInt = PlayerPrefs.GetInt("SelectedLanguage", 0);
        CurrentLanguage = (Language)langInt;
    }

    public void SetLanguage(Language lang)
    {
        CurrentLanguage = lang;
        PlayerPrefs.SetInt("SelectedLanguage", (int)lang);
        PlayerPrefs.Save();

        // GameManager 및 UI 업데이트 호출
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.UpdateUI();
            gm.UpdateGachaUI();
        }

        TabManager tm = FindObjectOfType<TabManager>();
        if (tm != null)
        {
            tm.UpdateTabLabels();
            tm.SwitchTab(tm.ActiveTab); // 현재 탭 상태와 텍스트 리로드
        }
    }

    public string Get(string key)
    {
        if (localizedStrings.ContainsKey(key))
        {
            return localizedStrings[key][CurrentLanguage];
        }
        return key;
    }

    public string GetUnitName(int index)
    {
        if (unitNames.ContainsKey(index))
        {
            return unitNames[index][CurrentLanguage];
        }
        return "Unknown Soldier";
    }

    public string GetEquipmentName(string originalName)
    {
        // 한국어와 영어 매핑
        var mapping = new Dictionary<string, Dictionary<Language, string>>()
        {
            { "E급 헌터 단검", new Dictionary<Language, string>() { { Language.Korean, "E급 헌터 단검" }, { Language.English, "E-Rank Hunter Dagger" } } },
            { "낡은 강철 장검", new Dictionary<Language, string>() { { Language.Korean, "낡은 강철 장검" }, { Language.English, "Old Steel Sword" } } },
            { "수습 마법사의 지팡이", new Dictionary<Language, string>() { { Language.Korean, "수습 마법사의 지팡이" }, { Language.English, "Apprentice Mage's Staff" } } },
            { "D급 헌터의 장검", new Dictionary<Language, string>() { { Language.Korean, "D급 헌터의 장검" }, { Language.English, "D-Rank Hunter's Longsword" } } },
            { "기사의 붉은 방패", new Dictionary<Language, string>() { { Language.Korean, "기사의 붉은 방패" }, { Language.English, "Knight's Red Shield" } } },
            { "정예 저격수의 활", new Dictionary<Language, string>() { { Language.Korean, "정예 저격수의 활" }, { Language.English, "Elite Sniper's Bow" } } },
            { "B급 암살자의 비수", new Dictionary<Language, string>() { { Language.Korean, "B급 암살자의 비수" }, { Language.English, "B-Rank Assassin's Stiletto" } } },
            { "하이오크 주술 보주", new Dictionary<Language, string>() { { Language.Korean, "하이오크 주술 보주" }, { Language.English, "High Orc Shaman's Orb" } } },
            { "성기사의 영광된 갑옷", new Dictionary<Language, string>() { { Language.Korean, "성기사의 영광된 갑옷" }, { Language.English, "Paladin's Glorious Armor" } } },
            { "카사카의 독니", new Dictionary<Language, string>() { { Language.Korean, "카사카의 독니" }, { Language.English, "Kasaka's Venom Fang" } } },
            { "악마왕의 단검", new Dictionary<Language, string>() { { Language.Korean, "악마왕의 단검" }, { Language.English, "Demon King's Dagger" } } }
        };

        if (mapping.ContainsKey(originalName))
        {
            return mapping[originalName][CurrentLanguage];
        }
        return originalName;
    }

    public string GetEquipmentDesc(string originalName, string bonusType, float bonusVal)
    {
        if (CurrentLanguage == Language.Korean)
        {
            if (bonusType == "Click") return $"클릭당 마력 획득 +{bonusVal}";
            if (bonusType == "LPS") return $"초당 마나 생산 +{bonusVal:N0}";
            return $"마력 오버로드/기본 배율 +{(bonusVal * 100):F0}% (+{bonusVal:F1})";
        }
        else
        {
            if (bonusType == "Click") return $"Click profit +{bonusVal}";
            if (bonusType == "LPS") return $"Mana per second +{bonusVal:N0}";
            return $"Mana Overload/Base Multiplier +{(bonusVal * 100):F0}% (+{bonusVal:F1})";
        }
    }
}
