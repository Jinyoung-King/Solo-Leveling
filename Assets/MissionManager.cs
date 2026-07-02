using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public enum MissionType { Beast, Gate, Summon }

// 리텐션 시스템: 일일 임무 + 연속 출석 보상.
// 날짜(yyyyMMdd) 기반으로 매일 리셋되며 PlayerPrefs 로 자체 저장한다.
public class MissionManager : MonoBehaviour
{
    private GameManager gameManager;

    private class Mission
    {
        public MissionType type;
        public int target;
        public int current;
        public bool claimed;
        public TextMeshProUGUI infoText;
        public Button claimButton;
        public TextMeshProUGUI claimLabel;
    }

    private readonly List<Mission> missions = new List<Mission>();

    // 연속 출석
    private int loginStreak = 1;
    private bool loginClaimedToday = false;

    private GameObject popup;
    private TextMeshProUGUI loginText;
    private Button loginButton;
    private TextMeshProUGUI loginBtnLabel;

    // PlayerPrefs 키
    const string KEY_DATE = "Daily_Date";
    const string KEY_STREAK = "Login_Streak";
    const string KEY_LOGIN_CLAIMED = "Login_ClaimedDate";

    public void Initialize(GameManager gm)
    {
        gameManager = gm;

        missions.Add(new Mission { type = MissionType.Beast, target = 3 });
        missions.Add(new Mission { type = MissionType.Gate, target = 2 });
        missions.Add(new Mission { type = MissionType.Summon, target = 10 });

        LoadState();
        CheckDailyReset();
        CreateUI();
        RefreshUI();
    }

    private string Today() => DateTime.Now.ToString("yyyyMMdd");
    private string Yesterday() => DateTime.Now.Date.AddDays(-1).ToString("yyyyMMdd");

    private void LoadState()
    {
        loginStreak = PlayerPrefs.GetInt(KEY_STREAK, 1);
        for (int i = 0; i < missions.Count; i++)
        {
            missions[i].current = PlayerPrefs.GetInt($"Mission_Prog_{i}", 0);
            missions[i].claimed = PlayerPrefs.GetInt($"Mission_Claimed_{i}", 0) == 1;
        }
        loginClaimedToday = PlayerPrefs.GetString(KEY_LOGIN_CLAIMED, "") == Today();
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(KEY_STREAK, loginStreak);
        for (int i = 0; i < missions.Count; i++)
        {
            PlayerPrefs.SetInt($"Mission_Prog_{i}", missions[i].current);
            PlayerPrefs.SetInt($"Mission_Claimed_{i}", missions[i].claimed ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    private void CheckDailyReset()
    {
        string stored = PlayerPrefs.GetString(KEY_DATE, "");
        string today = Today();
        if (stored == today) return; // 같은 날 -> 유지

        // 새로운 날
        if (stored == Yesterday()) loginStreak += 1; // 연속 출석
        else loginStreak = 1;                        // 끊김

        foreach (var m in missions) { m.current = 0; m.claimed = false; }
        loginClaimedToday = false;

        PlayerPrefs.SetString(KEY_DATE, today);
        PlayerPrefs.SetInt(KEY_STREAK, loginStreak);
        PlayerPrefs.SetString(KEY_LOGIN_CLAIMED, ""); // 오늘 출석 보상 미수령
        SaveState();
    }

    // 이벤트 훅
    public void AddProgress(MissionType type)
    {
        bool changed = false;
        foreach (var m in missions)
        {
            if (m.type == type && !m.claimed && m.current < m.target)
            {
                m.current++;
                changed = true;
            }
        }
        if (changed)
        {
            SaveState();
            RefreshUI();
        }
    }

    private void ClaimMission(Mission m)
    {
        if (m.current < m.target || m.claimed) return;

        long reward = Math.Max(gameManager.GetTotalRevenue() * 180, (long)gameManager.serverLevel * 1000 + 1000);
        gameManager.logs += reward;
        m.claimed = true;

        bool en = IsEnglish();
        gameManager.terminalManager?.AddLog($"<color=#00FF88><b>{(en ? "[MISSION] Reward" : "[임무] 보상")} +{gameManager.FormatNumber(reward)}</b></color>");

        // 모든 임무 완료 시 어둠의 징표 보너스
        bool allDone = true;
        foreach (var mm in missions) if (!mm.claimed) { allDone = false; break; }
        if (allDone)
        {
            gameManager.goldenDisks += 1;
            gameManager.terminalManager?.AddLog($"<color=yellow><b>{(en ? "[DAILY COMPLETE] Dark Mark +1!" : "[일일 완료] 어둠의 징표 +1!")}</b></color>");
        }

        SaveState();
        gameManager.UpdateUI();
        gameManager.SaveGame();
        RefreshUI();
    }

    private void ClaimLogin()
    {
        if (loginClaimedToday) return;

        long reward = Math.Max(gameManager.GetTotalRevenue() * 300, (long)gameManager.serverLevel * 2000 + 2000);
        gameManager.logs += reward;

        bool en = IsEnglish();
        gameManager.terminalManager?.AddLog($"<color=#FFD700><b>{(en ? $"[LOGIN Day {loginStreak}] +" : $"[출석 {loginStreak}일차] +")}{gameManager.FormatNumber(reward)}</b></color>");

        if (loginStreak % 7 == 0)
        {
            gameManager.goldenDisks += 1;
            gameManager.terminalManager?.AddLog($"<color=yellow><b>{(en ? "[7-DAY STREAK] Dark Mark +1!" : "[7일 연속] 어둠의 징표 +1!")}</b></color>");
        }

        loginClaimedToday = true;
        PlayerPrefs.SetString(KEY_LOGIN_CLAIMED, Today());
        PlayerPrefs.Save();

        gameManager.UpdateUI();
        gameManager.SaveGame();
        RefreshUI();
    }

    public void OpenPopup()
    {
        if (popup != null)
        {
            popup.SetActive(true);
            popup.transform.SetAsLastSibling();
            RefreshUI();
        }
    }

    public void ClosePopup()
    {
        if (popup != null) popup.SetActive(false);
    }

    private bool IsEnglish() => LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == Language.English;

    private string MissionName(Mission m, bool en)
    {
        switch (m.type)
        {
            case MissionType.Beast: return en ? $"Purge {m.target} beasts" : $"마수 {m.target}마리 처치";
            case MissionType.Gate: return en ? $"Clear {m.target} gates" : $"게이트 {m.target}회 클리어";
            case MissionType.Summon: return en ? $"Summon {m.target} shadows" : $"그림자 {m.target}회 소환";
        }
        return "";
    }

    // ---------- UI ----------
    private void CreateUI()
    {
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        bool en = IsEnglish();
        TMP_FontAsset font = gameManager.scoreText != null ? gameManager.scoreText.font : null;

        // HUD 열기 버튼 (우측 상단, 언어 버튼 아래)
        GameObject openGo = new GameObject("Btn_Open_Missions", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        openGo.transform.SetParent(canvas.transform, false);
        RectTransform or = openGo.GetComponent<RectTransform>();
        or.anchorMin = new Vector2(1f, 1f); or.anchorMax = new Vector2(1f, 1f); or.pivot = new Vector2(1f, 1f);
        or.anchoredPosition = new Vector2(-20f, -95f);
        or.sizeDelta = new Vector2(150f, 64f);
        openGo.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.25f, 0.95f);
        MakeText(openGo.transform, en ? "Missions" : "일일 임무", 18, font, TextAlignmentOptions.Center);
        openGo.GetComponent<Button>().onClick.AddListener(OpenPopup);

        // 팝업
        popup = new GameObject("Panel_Missions", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        popup.transform.SetParent(canvas.transform, false);
        RectTransform pr = popup.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 0.5f); pr.anchorMax = new Vector2(0.5f, 0.5f); pr.pivot = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = Vector2.zero;
        pr.sizeDelta = new Vector2(880f, 1000f);
        popup.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.97f);

        // 타이틀
        var title = MakeText(popup.transform, en ? "DAILY MISSIONS" : "일일 임무", 34, font, TextAlignmentOptions.Center);
        SetTop(title.rectTransform, -30f, 60f);
        title.color = Color.yellow;

        // 연속 출석 영역
        loginText = MakeText(popup.transform, "", 24, font, TextAlignmentOptions.Left);
        SetTop(loginText.rectTransform, -120f, 60f, 40f, -220f);
        loginButton = MakeButton(popup.transform, en ? "Claim" : "받기", font, out loginBtnLabel, new Color(0.85f, 0.6f, 0.1f, 1f));
        RectTransform lbr = loginButton.GetComponent<RectTransform>();
        lbr.anchorMin = new Vector2(1f, 1f); lbr.anchorMax = new Vector2(1f, 1f); lbr.pivot = new Vector2(1f, 1f);
        lbr.anchoredPosition = new Vector2(-40f, -110f);
        lbr.sizeDelta = new Vector2(150f, 70f);
        loginButton.onClick.AddListener(ClaimLogin);

        // 임무 행들
        float y = -230f;
        for (int i = 0; i < missions.Count; i++)
        {
            Mission m = missions[i];

            m.infoText = MakeText(popup.transform, "", 22, font, TextAlignmentOptions.Left);
            SetTop(m.infoText.rectTransform, y, 70f, 40f, -220f);

            Mission captured = m;
            m.claimButton = MakeButton(popup.transform, en ? "Claim" : "받기", font, out m.claimLabel, new Color(0.2f, 0.5f, 0.85f, 1f));
            RectTransform cbr = m.claimButton.GetComponent<RectTransform>();
            cbr.anchorMin = new Vector2(1f, 1f); cbr.anchorMax = new Vector2(1f, 1f); cbr.pivot = new Vector2(1f, 1f);
            cbr.anchoredPosition = new Vector2(-40f, y + 5f);
            cbr.sizeDelta = new Vector2(150f, 70f);
            m.claimButton.onClick.AddListener(() => ClaimMission(captured));

            y -= 100f;
        }

        // 닫기
        Button close = MakeButton(popup.transform, en ? "Close" : "닫기", font, out _, new Color(0.3f, 0.3f, 0.3f, 1f));
        RectTransform clr = close.GetComponent<RectTransform>();
        clr.anchorMin = new Vector2(0.5f, 0f); clr.anchorMax = new Vector2(0.5f, 0f); clr.pivot = new Vector2(0.5f, 0f);
        clr.anchoredPosition = new Vector2(0f, 40f);
        clr.sizeDelta = new Vector2(400f, 80f);
        close.onClick.AddListener(ClosePopup);

        popup.SetActive(false);
    }

    private void RefreshUI()
    {
        bool en = IsEnglish();

        if (loginText != null)
        {
            string streakStr = en ? $"Login Streak: Day {loginStreak}" : $"연속 출석: {loginStreak}일차";
            loginText.text = streakStr;
        }
        if (loginButton != null)
        {
            loginButton.interactable = !loginClaimedToday;
            if (loginBtnLabel != null) loginBtnLabel.text = loginClaimedToday ? (en ? "Done" : "완료") : (en ? "Claim" : "받기");
        }

        foreach (var m in missions)
        {
            if (m.infoText != null)
            {
                string nm = MissionName(m, en);
                string prog = $"{Mathf.Min(m.current, m.target)}/{m.target}";
                string color = m.current >= m.target ? "#00FF00" : "#FFFFFF";
                m.infoText.text = $"<color={color}>{nm}  ({prog})</color>";
            }
            if (m.claimButton != null)
            {
                bool canClaim = m.current >= m.target && !m.claimed;
                m.claimButton.interactable = canClaim;
                if (m.claimLabel != null)
                    m.claimLabel.text = m.claimed ? (en ? "Done" : "완료") : (en ? "Claim" : "받기");
            }
        }
    }

    // ---------- UI 헬퍼 ----------
    private TextMeshProUGUI MakeText(Transform parent, string text, float size, TMP_FontAsset font, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(-40f, 50f);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize = size; t.color = Color.white; t.alignment = align; t.text = text; t.raycastTarget = false;
        if (font != null) t.font = font;
        return t;
    }

    private void SetTop(RectTransform r, float posY, float height, float leftInset = 0f, float widthDelta = -40f)
    {
        r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(leftInset / 2f, posY);
        r.sizeDelta = new Vector2(widthDelta, height);
    }

    private Button MakeButton(Transform parent, string label, TMP_FontAsset font, out TextMeshProUGUI outLabel, Color color)
    {
        GameObject go = new GameObject("Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        outLabel = MakeText(go.transform, label, 22, font, TextAlignmentOptions.Center);
        RectTransform lr = outLabel.rectTransform;
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.pivot = new Vector2(0.5f, 0.5f);
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
        return go.GetComponent<Button>();
    }
}
