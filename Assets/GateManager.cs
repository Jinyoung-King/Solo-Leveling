using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 게이트/던전 진행 시스템.
// 각 게이트는 HP를 가진 보스이며, 클릭(클릭 데미지)과 초당생산(지속 딜)으로 HP를 깎는다.
// 클리어 시 영구 마나 배수(+RewardPerGate)를 얻고 다음 게이트로 진행한다.
public class GateManager : MonoBehaviour
{
    private GameManager gameManager;

    private double bossMaxHp;
    private double bossCurrentHp;

    // --- 튜닝 상수 ---
    private const double BaseHp = 20.0;      // 1게이트 보스 HP
    private const double HpGrowth = 1.65;    // 게이트당 HP 증가율
    private const float RewardPerGate = 0.05f; // 게이트당 영구 마나/딜 +5%

    // --- UI ---
    private RectTransform fillRect;
    private TextMeshProUGUI hpText;
    private TextMeshProUGUI gateLabel;

    // 랭크업/보스 격파 배너
    private CanvasGroup bannerGroup;
    private TextMeshProUGUI bannerText;
    private float bannerHold;

    private static readonly string[] RanksKr = { "E", "D", "C", "B", "A", "S" };
    private static readonly string[] BossKr = { "고블린", "오크", "리자드맨", "오우거", "나가", "자이언트 앤트", "몬스터 군주" };
    private static readonly string[] BossEn = { "Goblin", "Orc", "Lizardman", "Ogre", "Naga", "Giant Ant", "Monster Monarch" };

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        if (gameManager.currentGate < 1) gameManager.currentGate = 1;
        RecomputeHp(true);
        CreateUI();
        RefreshUI();
    }

    private void RecomputeHp(bool fullHeal)
    {
        bossMaxHp = BaseHp * System.Math.Pow(HpGrowth, gameManager.currentGate - 1);
        if (fullHeal || bossCurrentHp > bossMaxHp) bossCurrentHp = bossMaxHp;
    }

    // 클릭/초당생산이 현재 게이트 보스에게 주는 데미지
    public void DealDamage(long amount)
    {
        if (gameManager == null || amount <= 0) return;

        bossCurrentHp -= amount;
        while (bossCurrentHp <= 0)
        {
            ClearGate();
        }
        RefreshUI();
    }

    private void ClearGate()
    {
        int cleared = gameManager.currentGate;
        gameManager.currentGate++;

        bool bossGate = (cleared % 5 == 0);
        int oldTier = (cleared - 1) / 5;
        int newTier = (gameManager.currentGate - 1) / 5;
        bool rankUp = newTier > oldTier;

        // 넘친 데미지를 다음 보스에게 이월
        double overflow = -bossCurrentHp;
        RecomputeHp(true);
        bossCurrentHp -= overflow;

        bool en = IsEnglish();
        string boss = BossName(cleared, en);
        string msg = en
            ? $"[GATE CLEARED] {RankName(cleared)}-Rank {boss} defeated! (+{RewardPerGate * 100f:F0}% Mana)"
            : $"[게이트 클리어] {RankName(cleared)}랭크 {boss} 처치! (마력 +{RewardPerGate * 100f:F0}%)";
        gameManager.terminalManager?.AddLog($"<color=#00FFAA><b>{msg}</b></color>");

        if (bossGate)
        {
            gameManager.goldenDisks += 1;
            long bonus = System.Math.Max(gameManager.GetTotalRevenue() * 60, (long)gameManager.serverLevel * 5000);
            gameManager.logs += bonus;

            string bmsg = en
                ? $"[BOSS GATE] Dark Mark +1 & {gameManager.FormatNumber(bonus)} Mana!"
                : $"[보스 게이트] 어둠의 징표 +1 & 마력 {gameManager.FormatNumber(bonus)}!";
            gameManager.terminalManager?.AddLog($"<color=yellow><b>{bmsg}</b></color>");
        }

        // 배너 연출 (랭크업 우선)
        if (rankUp)
        {
            ShowBanner(en ? $"{RankName(gameManager.currentGate)}-RANK GATE!" : $"{RankName(gameManager.currentGate)}랭크 게이트 진입!", Color.cyan);
        }
        else if (bossGate)
        {
            ShowBanner(en ? $"BOSS DOWN! {RankName(cleared)}-Rank" : $"보스 격파! {RankName(cleared)}랭크", Color.yellow);
        }

        if (gameManager.buttonShaker != null) gameManager.buttonShaker.Shake();

        gameManager.ReportGateClear();
        gameManager.UpdateUI();
        gameManager.SaveGame();
    }

    private void ShowBanner(string msg, Color color)
    {
        if (bannerText == null) return;
        bannerText.text = msg;
        bannerText.color = color;
        if (bannerGroup != null) bannerGroup.alpha = 1f;
        bannerHold = 1.6f;
    }

    void Update()
    {
        if (bannerGroup != null && bannerGroup.alpha > 0f)
        {
            if (bannerHold > 0f) bannerHold -= Time.deltaTime;
            else bannerGroup.alpha = Mathf.MoveTowards(bannerGroup.alpha, 0f, Time.deltaTime * 0.8f);
        }
    }

    // 게이트 클리어로 얻은 영구 배수 (경제 전반에 반영)
    public float GetGateMultiplier()
    {
        if (gameManager == null) return 1f;
        return 1f + RewardPerGate * (gameManager.currentGate - 1);
    }

    // 프레스티지/초기화 시 게이트 UI 재계산 (진행도 자체는 초기화 정책에 따름)
    public void ResetToCurrent()
    {
        RecomputeHp(true);
        RefreshUI();
    }

    // ---------- 헬퍼 ----------
    private bool IsEnglish()
    {
        return LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == Language.English;
    }

    private string RankName(int gate)
    {
        int tier = (gate - 1) / 5;
        return tier < RanksKr.Length ? RanksKr[tier] : (IsEnglish() ? "National" : "국가권력급");
    }

    private string BossName(int gate, bool en)
    {
        int tier = (gate - 1) / 5;
        string[] arr = en ? BossEn : BossKr;
        if (tier >= arr.Length) tier = arr.Length - 1;
        return arr[tier];
    }

    // ---------- UI ----------
    private void CreateUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // 상단 보스 바 컨테이너
        GameObject panel = new GameObject("Panel_GateBoss", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 1f);
        pr.anchorMax = new Vector2(0.5f, 1f);
        pr.pivot = new Vector2(0.5f, 1f);
        pr.anchoredPosition = new Vector2(0f, -165f);
        pr.sizeDelta = new Vector2(780f, 120f);
        panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.55f);

        // 게이트/보스 라벨
        GameObject labelGo = new GameObject("Txt_GateLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(panel.transform, false);
        RectTransform lr = labelGo.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 1f);
        lr.anchorMax = new Vector2(1f, 1f);
        lr.pivot = new Vector2(0.5f, 1f);
        lr.anchoredPosition = new Vector2(0f, -8f);
        lr.sizeDelta = new Vector2(-24f, 52f);
        gateLabel = labelGo.GetComponent<TextMeshProUGUI>();
        gateLabel.fontSize = 24;
        gateLabel.color = Color.white;
        gateLabel.alignment = TextAlignmentOptions.Center;
        gateLabel.raycastTarget = false;

        // HP 바 배경
        GameObject barBg = new GameObject("Bar_Bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barBg.transform.SetParent(panel.transform, false);
        RectTransform bgr = barBg.GetComponent<RectTransform>();
        bgr.anchorMin = new Vector2(0f, 0f);
        bgr.anchorMax = new Vector2(1f, 0f);
        bgr.pivot = new Vector2(0.5f, 0f);
        bgr.anchoredPosition = new Vector2(0f, 14f);
        bgr.sizeDelta = new Vector2(-24f, 44f);
        barBg.GetComponent<Image>().color = new Color(0.15f, 0.02f, 0.02f, 0.9f);
        barBg.GetComponent<Image>().raycastTarget = false;

        // HP 채움(앵커 기반, 스프라이트 불필요)
        GameObject fill = new GameObject("Bar_Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(barBg.transform, false);
        fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(0.85f, 0.15f, 0.2f, 0.95f);
        fillImg.raycastTarget = false;

        // HP 수치 텍스트 (바 위에 겹침)
        GameObject hpGo = new GameObject("Txt_Hp", typeof(RectTransform), typeof(TextMeshProUGUI));
        hpGo.transform.SetParent(barBg.transform, false);
        RectTransform hr = hpGo.GetComponent<RectTransform>();
        hr.anchorMin = Vector2.zero;
        hr.anchorMax = Vector2.one;
        hr.offsetMin = Vector2.zero;
        hr.offsetMax = Vector2.zero;
        hpText = hpGo.GetComponent<TextMeshProUGUI>();
        hpText.fontSize = 20;
        hpText.color = Color.white;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.raycastTarget = false;

        // 폰트 상속
        if (gameManager.scoreText != null)
        {
            gateLabel.font = gameManager.scoreText.font;
            hpText.font = gameManager.scoreText.font;
        }

        // 게이트 탭에서만 표시되도록 TabManager에 등록
        gameManager.gateBossPanel = panel;

        // 랭크업/보스 격파 배너 (캔버스 직속 - 어느 탭에서도 표시)
        GameObject bGo = new GameObject("Txt_GateBanner", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        bGo.transform.SetParent(canvas.transform, false);
        RectTransform brt = bGo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0f, 250f);
        brt.sizeDelta = new Vector2(940f, 160f);
        bannerText = bGo.GetComponent<TextMeshProUGUI>();
        bannerText.fontSize = 48;
        bannerText.fontStyle = FontStyles.Bold;
        bannerText.color = Color.yellow;
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.raycastTarget = false;
        if (gameManager.scoreText != null) bannerText.font = gameManager.scoreText.font;
        bannerGroup = bGo.GetComponent<CanvasGroup>();
        bannerGroup.alpha = 0f;
        bannerGroup.interactable = false;
        bannerGroup.blocksRaycasts = false;
    }

    private void RefreshUI()
    {
        if (gameManager == null) return;

        bool en = IsEnglish();
        int gate = gameManager.currentGate;

        if (gateLabel != null)
        {
            string rankGate = en
                ? $"{RankName(gate)}-Rank Gate · Lv.{gate}"
                : $"{RankName(gate)}랭크 게이트 · Lv.{gate}";
            gateLabel.text = $"<size=90%>{rankGate}</size>\n<b>{BossName(gate, en)}</b>";
        }

        if (fillRect != null)
        {
            float ratio = bossMaxHp > 0 ? Mathf.Clamp01((float)(bossCurrentHp / bossMaxHp)) : 0f;
            fillRect.anchorMax = new Vector2(ratio, 1f);
        }

        if (hpText != null)
        {
            long cur = (long)System.Math.Max(0, System.Math.Ceiling(bossCurrentHp));
            hpText.text = $"{gameManager.FormatNumber(cur)} / {gameManager.FormatNumber((long)System.Math.Ceiling(bossMaxHp))}";
        }
    }
}
