using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// 그림자 군대 심화: 유닛 클래스 편성 시너지.
// 보유 종류 수 + 완성 클래스 + 군단장 보유에 따라 영구 생산 배수를 부여한다.
public class ArmyManager : MonoBehaviour
{
    private GameManager gameManager;

    private class ShadowClass
    {
        public string nameKr;
        public string nameEn;
        public int[] unitIndices;
    }

    private readonly List<ShadowClass> classes = new List<ShadowClass>
    {
        new ShadowClass { nameKr = "근접",   nameEn = "Melee",     unitIndices = new[] { 0, 5 } },
        new ShadowClass { nameKr = "원거리", nameEn = "Ranged",    unitIndices = new[] { 1, 2, 6 } },
        new ShadowClass { nameKr = "마법",   nameEn = "Magic",     unitIndices = new[] { 3, 7 } },
        new ShadowClass { nameKr = "암살",   nameEn = "Assassin",  unitIndices = new[] { 4 } },
        new ShadowClass { nameKr = "군단장", nameEn = "Commander", unitIndices = new[] { 8, 9 } },
    };

    private const float PerTypeBonus = 0.03f;   // 보유 종류당 +3%
    private const float FullClassBonus = 0.10f;  // 완성 클래스당 +10%
    private const float PerCommanderBonus = 0.01f; // 군단장(이그리트/베르) 보유 수당 +1%
    private const int CommanderCap = 100;

    private TextMeshProUGUI panelText;
    private float refreshTimer = 0f;

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        CreateUI();
        RefreshSynergy();
    }

    private bool HasUnits()
    {
        return gameManager != null && gameManager.units != null && gameManager.units.Count >= 10;
    }

    private int OwnedTypeCount()
    {
        int n = 0;
        foreach (var u in gameManager.units) if (u.count > 0) n++;
        return n;
    }

    private bool IsClassFull(ShadowClass c)
    {
        foreach (int idx in c.unitIndices)
        {
            if (idx >= gameManager.units.Count || gameManager.units[idx].count <= 0) return false;
        }
        return true;
    }

    private int CommanderCount()
    {
        int total = 0;
        foreach (int idx in new[] { 8, 9 })
            if (idx < gameManager.units.Count) total += gameManager.units[idx].count;
        return total;
    }

    // 경제에 반영되는 시너지 배수
    public float GetSynergyMultiplier()
    {
        if (!HasUnits()) return 1f;

        float bonus = PerTypeBonus * OwnedTypeCount();
        foreach (var c in classes) if (IsClassFull(c)) bonus += FullClassBonus;
        bonus += PerCommanderBonus * Mathf.Min(CommanderCount(), CommanderCap);

        return 1f + bonus;
    }

    void Update()
    {
        // 1초마다 패널 갱신 (구매 시 즉시 반영은 UpdateUI 경유)
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= 1f)
        {
            refreshTimer = 0f;
            RefreshSynergy();
        }
    }

    public void RefreshSynergy()
    {
        if (panelText == null || !HasUnits()) return;

        bool en = IsEnglish();
        float mult = GetSynergyMultiplier();
        int owned = OwnedTypeCount();
        int fullClasses = 0;
        foreach (var c in classes) if (IsClassFull(c)) fullClasses++;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(en ? $"<b>Army Synergy  x{mult:F2}</b>\n" : $"<b>군대 시너지  x{mult:F2}</b>\n");
        sb.Append(en
            ? $"<size=80%>Shadows {owned}/10 · Sets {fullClasses}/{classes.Count}</size>\n"
            : $"<size=80%>보유 그림자 {owned}/10 · 완성 편성 {fullClasses}/{classes.Count}</size>\n");

        for (int i = 0; i < classes.Count; i++)
        {
            bool full = IsClassFull(classes[i]);
            string col = full ? "#00FF66" : "#666666";
            string nm = en ? classes[i].nameEn : classes[i].nameKr;
            sb.Append($"<color={col}>{nm}</color>");
            if (i < classes.Count - 1) sb.Append("  ");
        }

        panelText.text = sb.ToString();
    }

    private bool IsEnglish() => LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == Language.English;

    // ---------- UI ----------
    private void CreateUI()
    {
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // 상단 시너지 패널 (그림자 탭 전용 - 게이트 보스 바와 탭이 달라 충돌 없음)
        GameObject panel = new GameObject("Panel_ArmySynergy", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 1f);
        pr.anchorMax = new Vector2(0.5f, 1f);
        pr.pivot = new Vector2(0.5f, 1f);
        pr.anchoredPosition = new Vector2(0f, 165f * -1f);
        pr.sizeDelta = new Vector2(780f, 150f);
        Image bg = panel.GetComponent<Image>();
        UITheme.Panel(bg, new Color(0.10f, 0.09f, 0.18f, 0.78f));
        bg.raycastTarget = false;

        GameObject txtGo = new GameObject("Txt_Synergy", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(panel.transform, false);
        RectTransform tr = txtGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(16f, 10f); tr.offsetMax = new Vector2(-16f, -10f);
        panelText = txtGo.GetComponent<TextMeshProUGUI>();
        panelText.fontSize = 22;
        panelText.color = Color.white;
        panelText.alignment = TextAlignmentOptions.Center;
        panelText.raycastTarget = false;
        if (gameManager.scoreText != null) panelText.font = gameManager.scoreText.font;

        gameManager.armySynergyPanel = panel;
    }
}
