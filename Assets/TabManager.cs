using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TabManager : MonoBehaviour
{
    private GameManager gameManager;

    private List<GameObject> portalElements = new List<GameObject>();
    private List<GameObject> armyElements = new List<GameObject>();
    private List<GameObject> gachaElements = new List<GameObject>();
    private List<GameObject> statusElements = new List<GameObject>();

    private List<Button> tabButtons = new List<Button>();
    private List<TextMeshProUGUI> tabTexts = new List<TextMeshProUGUI>();

    public int ActiveTab { get; private set; } = 0; // 0: Portal, 1: Army, 2: Gacha, 3: Status

    public void Initialize(GameManager gm)
    {
        gameManager = gm;

        // 1. Populate element groups (do NOT reparent to preserve layout/draw order)
        if (gameManager.serverButtonTr != null) portalElements.Add(gameManager.serverButtonTr.gameObject);
        if (gameManager.coffeeBtn != null) portalElements.Add(gameManager.coffeeBtn.gameObject);
        if (gameManager.coffeeSlider != null) portalElements.Add(gameManager.coffeeSlider.gameObject);
        if (gameManager.heatSlider != null) portalElements.Add(gameManager.heatSlider.gameObject);

        if (gameManager.shopContent != null)
        {
            Transform scrollView = gameManager.shopContent.parent.parent; // Scroll View
            if (scrollView != null)
            {
                armyElements.Add(scrollView.gameObject);
            }
        }

        // 군대 시너지 패널은 그림자(Army) 탭에서만 표시
        if (gameManager.armySynergyPanel != null)
        {
            armyElements.Add(gameManager.armySynergyPanel);
        }

        if (gameManager.gachaPanel != null)
        {
            gachaElements.Add(gameManager.gachaPanel);
        }

        // NOTE: heatSlider (Slider_Heat) is a child of upgradeButton (Btn_Upgrade_CPU) in the scene,
        // and both live in the central play area with the server button. They must share a tab,
        // otherwise deactivating the parent hides the child regardless of its own active flag.
        if (gameManager.upgradeButton != null)
        {
            portalElements.Add(gameManager.upgradeButton.gameObject);
        }

        // 게이트 보스 HP 바 / 군주 스킬바는 게이트(Gate) 탭에서만 표시
        if (gameManager.gateBossPanel != null)
        {
            portalElements.Add(gameManager.gateBossPanel);
        }
        if (gameManager.skillBarPanel != null)
        {
            portalElements.Add(gameManager.skillBarPanel);
        }

        // Status (Monarch) tab hosts only the prestige/migration panel.
        if (gameManager.migrationPanel != null)
        {
            statusElements.Add(gameManager.migrationPanel);
        }

        // 2. Create the Bottom Tab Bar
        CreateTabBar();

        // 3. Set default active tab (Portal)
        SwitchTab(0);
    }

    private void CreateTabBar()
    {
        // Find Canvas in scene since GameManager might be a root object
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Bottom Tab Panel
        GameObject tabBarGo = new GameObject("Panel_TabBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tabBarGo.transform.SetParent(canvas.transform, false);

        RectTransform barRect = tabBarGo.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 0);
        barRect.anchorMax = new Vector2(1, 0);
        barRect.pivot = new Vector2(0.5f, 0);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = new Vector2(0, 120); // 120px height
        barRect.offsetMin = new Vector2(0, 0);
        barRect.offsetMax = new Vector2(0, 120);

        // Keep the tab bar rendered above all toggled content.
        tabBarGo.transform.SetAsLastSibling();

        Image barImg = tabBarGo.GetComponent<Image>();
        UITheme.Panel(barImg, UITheme.BarBg);

        float buttonWidth = 1f / 4f;

        for (int i = 0; i < 4; i++)
        {
            GameObject btnGo = new GameObject($"Btn_Tab_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(tabBarGo.transform, false);

            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(i * buttonWidth, 0);
            btnRect.anchorMax = new Vector2((i + 1) * buttonWidth, 1);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.offsetMin = new Vector2(10, 10);
            btnRect.offsetMax = new Vector2(-10, -10);

            Image btnImg = btnGo.GetComponent<Image>();

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI txt = textGo.GetComponent<TextMeshProUGUI>();
            txt.fontSize = 22;
            txt.color = UITheme.TextMain;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            if (gameManager.scoreText != null) txt.font = gameManager.scoreText.font;

            int tabIndex = i;
            Button btn = btnGo.GetComponent<Button>();
            UITheme.StyleButton(btn, UITheme.PanelSoft);
            btn.onClick.AddListener(() => SwitchTab(tabIndex));

            tabButtons.Add(btn);
            tabTexts.Add(txt);
        }

        UpdateTabLabels();
    }

    public void UpdateTabLabels()
    {
        if (tabTexts.Count < 4) return;

        bool isEnglish = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == Language.English;
        string[] names = isEnglish ? new[] { "Gate", "Army", "Armory", "Monarch" } : new[] { "게이트", "그림자", "무기고", "군주 정보" };

        for (int i = 0; i < 4; i++)
        {
            tabTexts[i].text = names[i];
        }
    }

    public void SwitchTab(int index)
    {
        ActiveTab = index;

        // Toggle active state of original components directly without reparenting
        foreach (var go in portalElements) if (go != null) go.SetActive(index == 0);
        foreach (var go in armyElements) if (go != null) go.SetActive(index == 1);
        foreach (var go in gachaElements) if (go != null) go.SetActive(index == 2);
        foreach (var go in statusElements) if (go != null) go.SetActive(index == 3);

        // Visual feedback for active button
        for (int i = 0; i < tabButtons.Count; i++)
        {
            Image img = tabButtons[i].GetComponent<Image>();
            if (i == index)
            {
                img.color = UITheme.Primary; // 활성 탭: 바이올렛
                tabTexts[i].color = UITheme.Gold;
            }
            else
            {
                img.color = UITheme.PanelSoft; // 비활성 탭
                tabTexts[i].color = UITheme.TextDim;
            }
        }

        // Special configurations per tab
        if (index == 2)
        {
            if (gameManager.gachaPanel != null)
            {
                gameManager.gachaPanel.SetActive(true);
                gameManager.UpdateGachaUI();
            }
        }
        else
        {
            if (gameManager.gachaPanel != null)
            {
                gameManager.gachaPanel.SetActive(false);
            }
        }

        if (index == 3)
        {
            if (gameManager.migrationPanel != null)
            {
                gameManager.migrationPanel.SetActive(true);
                
                // Reposition only inside Status tab area
                RectTransform mRect = gameManager.migrationPanel.GetComponent<RectTransform>();
                mRect.anchorMin = new Vector2(0.5f, 0.5f);
                mRect.anchorMax = new Vector2(0.5f, 0.5f);
                mRect.pivot = new Vector2(0.5f, 0.5f);
                mRect.anchoredPosition = new Vector2(0, 100);
                mRect.sizeDelta = new Vector2(800, 700);

                if (gameManager.migrationInfoText != null)
                {
                    int potentialDisks = (int)Mathf.Sqrt(gameManager.logs / 1000000f);
                    string formatStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("prestige_popup_desc") : "그림자 영토를 다음 차원 게이트로 이전하겠습니까?\n\n현재 그림자 군대 데이터는 초기화되지만 어둠의 징표 {0}개를 획득합니다.\n\n현재 보유: {1}개\n총 마력 보너스: +{2}%";
                    gameManager.migrationInfoText.text = string.Format(formatStr, potentialDisks, gameManager.goldenDisks, (gameManager.goldenDisks + potentialDisks) * 10);
                }
            }
        }
        else
        {
            if (gameManager.migrationPanel != null)
            {
                gameManager.migrationPanel.SetActive(false);
            }
        }
    }
}
