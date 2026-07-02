using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

// 군주 액티브 스킬바: 쿨다운 기반 액티브 스킬 버튼들을 런타임에 생성한다.
// 게이트 보스 전투를 능동적으로 만드는 것이 목적.
public class SkillBarManager : MonoBehaviour
{
    private GameManager gameManager;

    private class Skill
    {
        public string nameKr;
        public string nameEn;
        public float cooldown;
        public float timer;        // 남은 쿨다운
        public Action effect;
        public Button button;
        public RectTransform cdOverlay;
        public TextMeshProUGUI cdText;
    }

    private readonly List<Skill> skills = new List<Skill>();

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        BuildSkills();
        CreateUI();
    }

    private void BuildSkills()
    {
        skills.Add(new Skill
        {
            nameKr = "군단\n총공격",
            nameEn = "Legion\nAssault",
            cooldown = 60f,
            effect = LegionAssault
        });
        skills.Add(new Skill
        {
            nameKr = "지배자의\n손길",
            nameEn = "Ruler's\nHand",
            cooldown = 40f,
            effect = RulersHand
        });
    }

    // 군단 총공격: 현재 초당생산의 30배(최소 보정)를 보스에게 즉시 타격 + 마력 획득
    private void LegionAssault()
    {
        if (gameManager == null) return;

        long burst = gameManager.GetTotalRevenue() * 30;
        long floor = (long)gameManager.serverLevel * 200;
        if (burst < floor) burst = floor;

        gameManager.logs += burst;
        gameManager.DealGateDamage(burst);
        gameManager.UpdateUI();

        if (gameManager.buttonShaker != null) gameManager.buttonShaker.Shake();

        bool en = IsEnglish();
        string msg = en
            ? $"[LEGION ASSAULT] Shadow army strikes for {gameManager.FormatNumber(burst)}!"
            : $"[군단 총공격] 그림자 군대의 일제 공격! {gameManager.FormatNumber(burst)} 피해!";
        gameManager.terminalManager?.AddLog($"<color=#FF66FF><b>{msg}</b></color>");
    }

    // 지배자의 손길: 8초간 자동 클릭
    private void RulersHand()
    {
        if (gameManager == null) return;
        StartCoroutine(AutoClickRoutine(8f, 0.1f));

        bool en = IsEnglish();
        string msg = en ? "[RULER'S HAND] Auto-strike for 8s!" : "[지배자의 손길] 8초간 자동 공격!";
        gameManager.terminalManager?.AddLog($"<color=#66CCFF><b>{msg}</b></color>");
    }

    private IEnumerator AutoClickRoutine(float duration, float interval)
    {
        float t = 0f;
        while (t < duration)
        {
            if (gameManager != null) gameManager.OnClickServer();
            t += interval;
            yield return new WaitForSeconds(interval);
        }
    }

    private bool IsEnglish()
    {
        return LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == Language.English;
    }

    void Update()
    {
        for (int i = 0; i < skills.Count; i++)
        {
            Skill s = skills[i];
            if (s.button == null) continue;

            if (s.timer > 0f)
            {
                s.timer -= Time.deltaTime;
                if (s.timer < 0f) s.timer = 0f;

                if (s.cdOverlay != null)
                {
                    float ratio = s.cooldown > 0 ? Mathf.Clamp01(s.timer / s.cooldown) : 0f;
                    s.cdOverlay.gameObject.SetActive(true);
                    s.cdOverlay.anchorMax = new Vector2(1f, ratio);
                }
                if (s.cdText != null) s.cdText.text = Mathf.CeilToInt(s.timer).ToString();
                if (s.button.interactable) s.button.interactable = false;
            }
            else
            {
                if (s.cdOverlay != null && s.cdOverlay.gameObject.activeSelf) s.cdOverlay.gameObject.SetActive(false);
                if (s.cdText != null && s.cdText.text.Length > 0) s.cdText.text = "";
                if (!s.button.interactable) s.button.interactable = true;
            }
        }
    }

    // ---------- UI ----------
    private void CreateUI()
    {
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject panel = new GameObject("Panel_SkillBar", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 0f);
        pr.anchorMax = new Vector2(0.5f, 0f);
        pr.pivot = new Vector2(0.5f, 0f);
        pr.anchoredPosition = new Vector2(0f, 320f); // 메시지 박스(140) 위쪽
        pr.sizeDelta = new Vector2(620f, 140f);

        Color[] colors = { UITheme.Primary, new Color32(0x1C, 0x6E, 0x8C, 255) };
        float slot = 1f / skills.Count;

        for (int i = 0; i < skills.Count; i++)
        {
            Skill s = skills[i];

            GameObject btnGo = new GameObject($"Btn_Skill_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panel.transform, false);
            RectTransform br = btnGo.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(i * slot, 0f);
            br.anchorMax = new Vector2((i + 1) * slot, 1f);
            br.offsetMin = new Vector2(10f, 10f);
            br.offsetMax = new Vector2(-10f, -10f);

            // 이름 라벨
            GameObject nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(btnGo.transform, false);
            RectTransform nr = nameGo.GetComponent<RectTransform>();
            nr.anchorMin = Vector2.zero; nr.anchorMax = Vector2.one;
            nr.offsetMin = Vector2.zero; nr.offsetMax = Vector2.zero;
            TextMeshProUGUI nameTxt = nameGo.GetComponent<TextMeshProUGUI>();
            nameTxt.fontSize = 22;
            nameTxt.color = Color.white;
            nameTxt.alignment = TextAlignmentOptions.Center;
            nameTxt.text = IsEnglish() ? s.nameEn : s.nameKr;
            nameTxt.raycastTarget = false;
            if (gameManager.scoreText != null) nameTxt.font = gameManager.scoreText.font;

            // 쿨다운 오버레이 (아래에서 위로 차오르는 어두운 막)
            GameObject ovGo = new GameObject("CdOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ovGo.transform.SetParent(btnGo.transform, false);
            RectTransform ovr = ovGo.GetComponent<RectTransform>();
            ovr.anchorMin = new Vector2(0f, 0f);
            ovr.anchorMax = new Vector2(1f, 1f);
            ovr.offsetMin = Vector2.zero; ovr.offsetMax = Vector2.zero;
            Image ovImg = ovGo.GetComponent<Image>();
            UITheme.Panel(ovImg, new Color(0f, 0f, 0f, 0.6f));
            ovImg.raycastTarget = false;
            ovGo.SetActive(false);
            s.cdOverlay = ovr;

            // 쿨다운 숫자
            GameObject cdGo = new GameObject("CdText", typeof(RectTransform), typeof(TextMeshProUGUI));
            cdGo.transform.SetParent(btnGo.transform, false);
            RectTransform cdr = cdGo.GetComponent<RectTransform>();
            cdr.anchorMin = Vector2.zero; cdr.anchorMax = Vector2.one;
            cdr.offsetMin = Vector2.zero; cdr.offsetMax = Vector2.zero;
            TextMeshProUGUI cdTxt = cdGo.GetComponent<TextMeshProUGUI>();
            cdTxt.fontSize = 34;
            cdTxt.color = Color.yellow;
            cdTxt.alignment = TextAlignmentOptions.Center;
            cdTxt.raycastTarget = false;
            cdTxt.text = "";
            if (gameManager.scoreText != null) cdTxt.font = gameManager.scoreText.font;
            s.cdText = cdTxt;

            Button btn = btnGo.GetComponent<Button>();
            UITheme.StyleButton(btn, colors[i % colors.Length]);
            Skill captured = s;
            btn.onClick.AddListener(() =>
            {
                if (captured.timer <= 0f)
                {
                    captured.effect?.Invoke();
                    captured.timer = captured.cooldown;
                }
            });
            s.button = btn;
        }

        // 게이트 탭에서만 표시되도록 등록
        gameManager.skillBarPanel = panel;
    }
}
