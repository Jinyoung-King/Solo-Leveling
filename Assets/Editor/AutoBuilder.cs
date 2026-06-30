using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.IO;

[InitializeOnLoad]
public class AutoBuilder
{
    static AutoBuilder()
    {
        // 유니티 에디터 리로드 시 트리거 파일이 있으면 자동 빌드 가동
        string triggerPath = Path.Combine(Directory.GetCurrentDirectory(), "build_trigger.txt");
        if (File.Exists(triggerPath))
        {
            try
            {
                string buildType = File.ReadAllText(triggerPath).Trim();
                File.Delete(triggerPath);
                Debug.Log($"[AutoBuilder] Trigger file detected! Target Build: {buildType}");
                
                if (buildType.ToLower() == "pc")
                {
                    EditorApplication.delayCall += PerformBuild;
                }
                else
                {
                    EditorApplication.delayCall += PerformAndroidBuild;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AutoBuilder] Failed to process trigger: " + e.Message);
            }
        }
    }

    [MenuItem("Tools/1. Setup InputField & Send Button Only")]
    public static void SetupOnly()
    {
        Debug.Log("[AutoBuilder] Starting UI setup with Mobile Send Button...");
        string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        TerminalManager terminalManager = Object.FindAnyObjectByType<TerminalManager>();
        if (terminalManager == null)
        {
            Debug.LogError("[AutoBuilder] TerminalManager not found in scene!");
            return;
        }

        // 1. InputField 생성 및 할당
        if (terminalManager.inputField == null)
        {
            TMP_InputField existingInputField = terminalManager.GetComponentInChildren<TMP_InputField>();
            if (existingInputField == null)
            {
                Canvas canvas = terminalManager.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("[AutoBuilder] Canvas not found in parent hierarchy!");
                    return;
                }

                // TMP_InputField 컨테이너 생성
                GameObject inputFieldGo = new GameObject("TerminalInputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
                inputFieldGo.transform.SetParent(terminalManager.transform, false);

                // RectTransform 조정 (오른쪽 110만큼 띄워서 전송 버튼 공간 확보)
                RectTransform rect = inputFieldGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(0, 0);
                rect.anchoredPosition = new Vector2(10, 10);
                rect.sizeDelta = new Vector2(-120, 40); // 오른쪽 110px 여백 + 왼쪽 10px 여백

                Image bgImage = inputFieldGo.GetComponent<Image>();
                bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                GameObject textAreaGo = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
                textAreaGo.transform.SetParent(inputFieldGo.transform, false);
                RectTransform textAreaRect = textAreaGo.GetComponent<RectTransform>();
                textAreaRect.anchorMin = Vector2.zero;
                textAreaRect.anchorMax = Vector2.one;
                textAreaRect.sizeDelta = new Vector2(-10, -10);

                GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(textAreaGo.transform, false);
                RectTransform textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                TextMeshProUGUI textMesh = textGo.GetComponent<TextMeshProUGUI>();
                textMesh.fontSize = 18;
                textMesh.color = Color.green;
                textMesh.extraPadding = true;

                GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
                placeholderGo.transform.SetParent(textAreaGo.transform, false);
                RectTransform placeholderRect = placeholderGo.GetComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.sizeDelta = Vector2.zero;

                TextMeshProUGUI placeholderMesh = placeholderGo.GetComponent<TextMeshProUGUI>();
                placeholderMesh.fontSize = 18;
                placeholderMesh.color = new Color(0, 1, 0, 0.4f);
                placeholderMesh.text = "Enter command here...";
                placeholderMesh.fontStyle = FontStyles.Italic;

                TMP_InputField inputField = inputFieldGo.GetComponent<TMP_InputField>();
                inputField.textViewport = textAreaRect;
                inputField.textComponent = textMesh;
                inputField.placeholder = placeholderMesh;

                if (terminalManager.terminalText != null)
                {
                    textMesh.font = terminalManager.terminalText.font;
                    placeholderMesh.font = terminalManager.terminalText.font;
                }

                terminalManager.inputField = inputField;
                Debug.Log("[AutoBuilder] TMP_InputField successfully created.");
            }
            else
            {
                terminalManager.inputField = existingInputField;
                Debug.Log("[AutoBuilder] Found existing TMP_InputField. Reference assigned.");
            }
        }

        // 2. 모바일 전송(Send) 버튼 생성 및 연결
        Button existingSendBtn = terminalManager.GetComponentInChildren<Button>(true);
        if (existingSendBtn == null)
        {
            Debug.Log("[AutoBuilder] Creating new Mobile Send Button...");
            GameObject sendBtnGo = new GameObject("TerminalSendButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            sendBtnGo.transform.SetParent(terminalManager.transform, false);

            // 전송 버튼 위치 조정 (우측 하단 고정)
            RectTransform sendRect = sendBtnGo.GetComponent<RectTransform>();
            sendRect.anchorMin = new Vector2(1, 0);
            sendRect.anchorMax = new Vector2(1, 0);
            sendRect.pivot = new Vector2(1, 0);
            sendRect.anchoredPosition = new Vector2(-10, 10);
            sendRect.sizeDelta = new Vector2(90, 40);

            // 버튼 색상 세팅
            Image btnImage = sendBtnGo.GetComponent<Image>();
            btnImage.color = new Color(0.12f, 0.45f, 0.22f, 1f); // 어두운 초록색 (터미널 테마)

            // 버튼 텍스트 추가
            GameObject btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(sendBtnGo.transform, false);
            RectTransform btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI btnText = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnText.fontSize = 16;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.text = "Send";
            btnText.fontStyle = FontStyles.Bold;
            if (terminalManager.terminalText != null)
            {
                btnText.font = terminalManager.terminalText.font;
            }

            Debug.Log("[AutoBuilder] TerminalSendButton successfully created.");
        }

        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            SetupGachaUI(gameManager);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AutoBuilder] Scene modified and saved.");
    }

    [MenuItem("Tools/2. Setup and Build PC (Windows EXE)")]
    public static void PerformBuild()
    {
        SetupOnly();

        Debug.Log("[AutoBuilder] Starting standalone PC build...");
        string scenePath = "Assets/Scenes/SampleScene.unity";
        string buildFolder = Path.Combine(Directory.GetCurrentDirectory(), "Build");
        if (!Directory.Exists(buildFolder))
        {
            Directory.CreateDirectory(buildFolder);
        }

        string buildPath = Path.Combine(buildFolder, "SoloLeveling.exe");
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { scenePath };
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[AutoBuilder] PC Build succeeded! Path: {buildPath}");
        }
        else
        {
            Debug.LogError($"[AutoBuilder] PC Build failed with result: {summary.result}");
        }
    }

    [MenuItem("Tools/3. Setup and Build Android APK")]
    public static void PerformAndroidBuild()
    {
        // Note: 기기/플랫폼 빌드 시 인풋 시스템 설정을 런타임에 강제 교정하면 에디터와 빌드 어셈블리 간의
        // RenderPipelines DebugActionDesc 클래스 레이아웃 불일치(serialization mismatch) 오류가 발생할 수 있습니다.
        // 따라서 인풋 설정은 Project Settings에서 직접 'Both' 또는 'Old'로 통일하여 설정하고, 빌드 스크립트에서는 강제 변경하지 않습니다.

        SetupOnly();

        Debug.Log("[AutoBuilder] Starting standalone Android build...");
        string scenePath = "Assets/Scenes/SampleScene.unity";
        string buildFolder = Path.Combine(Directory.GetCurrentDirectory(), "Build");
        if (!Directory.Exists(buildFolder))
        {
            Directory.CreateDirectory(buildFolder);
        }

        string buildPath = Path.Combine(buildFolder, "SoloLeveling.apk");
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { scenePath };
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[AutoBuilder] Android Build succeeded! Path: {buildPath}");
        }
        else
        {
            Debug.LogError($"[AutoBuilder] Android Build failed with result: {summary.result}");
        }
    }

    public static void SetupGachaUI(GameManager gameManager)
    {
        if (gameManager == null) return;
        
        Canvas canvas = gameManager.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // 1. Gacha 메인 팝업 패널 생성
        if (gameManager.gachaPanel == null)
        {
            Debug.Log("[AutoBuilder] Creating Gacha Popup Panel...");
            GameObject gachaGo = new GameObject("Panel_Gacha", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gachaGo.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = gachaGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(900, 1300);

            Image bgImg = gachaGo.GetComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f); // 어두운 블랙

            // (1) 타이틀 텍스트
            GameObject titleGo = new GameObject("Txt_Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(gachaGo.transform, false);
            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -40);
            titleRect.sizeDelta = new Vector2(0, 60);

            TextMeshProUGUI titleText = titleGo.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 32;
            titleText.color = Color.yellow;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.text = "🎰 <b>HUNTER EQUIPMENT GACHA</b> 🎰";

            // (2) 결과 표시 영역
            GameObject resultGo = new GameObject("Txt_Result", typeof(RectTransform), typeof(TextMeshProUGUI));
            resultGo.transform.SetParent(gachaGo.transform, false);
            RectTransform resultRect = resultGo.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0, 1);
            resultRect.anchorMax = new Vector2(1, 1);
            resultRect.pivot = new Vector2(0.5f, 1);
            resultRect.anchoredPosition = new Vector2(0, -180);
            resultRect.sizeDelta = new Vector2(-40, 180);

            TextMeshProUGUI resultText = resultGo.GetComponent<TextMeshProUGUI>();
            resultText.fontSize = 24;
            resultText.color = Color.white;
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.text = "Mana를 소모하여\n헌터 무기 및 장비를 뽑으세요!\n\n<size=80%>(1회 50,000 Mana ➔ 뽑을 때마다 상승)</size>";
            gameManager.gachaResultText = resultText;

            // (3) 뽑기(Draw) 버튼
            GameObject drawBtnGo = new GameObject("Btn_Draw_Gacha", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            drawBtnGo.transform.SetParent(gachaGo.transform, false);
            RectTransform drawRect = drawBtnGo.GetComponent<RectTransform>();
            drawRect.anchorMin = new Vector2(0.5f, 0);
            drawRect.anchorMax = new Vector2(0.5f, 0);
            drawRect.pivot = new Vector2(0.5f, 0);
            drawRect.anchoredPosition = new Vector2(0, 240);
            drawRect.sizeDelta = new Vector2(400, 100);

            Image drawBtnImg = drawBtnGo.GetComponent<Image>();
            drawBtnImg.color = new Color(0.85f, 0.45f, 0f, 1f); // 오렌지/황금색 버튼

            GameObject drawBtnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            drawBtnTextGo.transform.SetParent(drawBtnGo.transform, false);
            RectTransform drawBtnTextRect = drawBtnTextGo.GetComponent<RectTransform>();
            drawBtnTextRect.anchorMin = Vector2.zero;
            drawBtnTextRect.anchorMax = Vector2.one;
            drawBtnTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI drawBtnText = drawBtnTextGo.GetComponent<TextMeshProUGUI>();
            drawBtnText.fontSize = 22;
            drawBtnText.color = Color.white;
            drawBtnText.alignment = TextAlignmentOptions.Center;
            drawBtnText.text = "🎰 1회 뽑기\nCost: 50.0K Mana";
            gameManager.gachaCostText = drawBtnText;

            Button drawBtn = drawBtnGo.GetComponent<Button>();
            drawBtn.onClick.AddListener(gameManager.DrawEquipmentGacha);

            // (4) 닫기(Close) 버튼
            GameObject closeBtnGo = new GameObject("Btn_Close_Gacha", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeBtnGo.transform.SetParent(gachaGo.transform, false);
            RectTransform closeRect = closeBtnGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0);
            closeRect.anchorMax = new Vector2(0.5f, 0);
            closeRect.pivot = new Vector2(0.5f, 0);
            closeRect.anchoredPosition = new Vector2(0, 80);
            closeRect.sizeDelta = new Vector2(400, 80);

            Image closeBtnImg = closeBtnGo.GetComponent<Image>();
            closeBtnImg.color = new Color(0.25f, 0.25f, 0.25f, 1f); // 회색

            GameObject closeBtnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeBtnTextGo.transform.SetParent(closeBtnGo.transform, false);
            RectTransform closeBtnTextRect = closeBtnTextGo.GetComponent<RectTransform>();
            closeBtnTextRect.anchorMin = Vector2.zero;
            closeBtnTextRect.anchorMax = Vector2.one;
            closeBtnTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI closeBtnText = closeBtnTextGo.GetComponent<TextMeshProUGUI>();
            closeBtnText.fontSize = 20;
            closeBtnText.color = Color.white;
            closeBtnText.alignment = TextAlignmentOptions.Center;
            closeBtnText.text = "닫기";

            Button closeBtn = closeBtnGo.GetComponent<Button>();
            closeBtn.onClick.AddListener(gameManager.CloseGachaPanel);

            // (5) 보유 장비 리스트 텍스트 (Txt_Equip_List)
            GameObject listGo = new GameObject("Txt_Equip_List", typeof(RectTransform), typeof(TextMeshProUGUI));
            listGo.transform.SetParent(gachaGo.transform, false);
            RectTransform listRect = listGo.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0.5f);
            listRect.anchorMax = new Vector2(1, 0.5f);
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.anchoredPosition = new Vector2(0, 50);
            listRect.sizeDelta = new Vector2(-60, 500);

            TextMeshProUGUI listText = listGo.GetComponent<TextMeshProUGUI>();
            listText.fontSize = 18;
            listText.color = Color.white;
            listText.alignment = TextAlignmentOptions.TopLeft;
            listText.text = "보유 장비 없음";
            gameManager.equipmentListText = listText;

            // 폰트 상속
            TerminalManager tm = Object.FindAnyObjectByType<TerminalManager>();
            if (tm != null && tm.terminalText != null)
            {
                titleText.font = tm.terminalText.font;
                resultText.font = tm.terminalText.font;
                drawBtnText.font = tm.terminalText.font;
                closeBtnText.font = tm.terminalText.font;
                listText.font = tm.terminalText.font;
            }

            gachaGo.SetActive(false);
            gameManager.gachaPanel = gachaGo;
            EditorUtility.SetDirty(gameManager);
        }

        // 2. 가차 팝업 열기 버튼 생성 (Btn_Open_Gacha)
        Button existingOpenBtn = null;
        Button[] allButtons = canvas.GetComponentsInChildren<Button>(true);
        foreach (var b in allButtons)
        {
            if (b.gameObject.name == "Btn_Open_Gacha")
            {
                existingOpenBtn = b;
                break;
            }
        }

        if (existingOpenBtn == null)
        {
            Debug.Log("[AutoBuilder] Creating Open Gacha Button on HUD...");
            GameObject openBtnGo = new GameObject("Btn_Open_Gacha", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            openBtnGo.transform.SetParent(canvas.transform, false);

            RectTransform openRect = openBtnGo.GetComponent<RectTransform>();
            openRect.anchorMin = new Vector2(0.5f, 1);
            openRect.anchorMax = new Vector2(0.5f, 1);
            openRect.pivot = new Vector2(0.5f, 1);
            openRect.anchoredPosition = new Vector2(300, -220); // 커피 버프 슬라이더 우측
            openRect.sizeDelta = new Vector2(150, 70);

            Image openImg = openBtnGo.GetComponent<Image>();
            openImg.color = new Color(0.85f, 0.55f, 0f, 1f); // 금색

            GameObject openTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            openTxtGo.transform.SetParent(openBtnGo.transform, false);
            RectTransform openTxtRect = openTxtGo.GetComponent<RectTransform>();
            openTxtRect.anchorMin = Vector2.zero;
            openTxtRect.anchorMax = Vector2.one;
            openTxtRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI openTxt = openTxtGo.GetComponent<TextMeshProUGUI>();
            openTxt.fontSize = 18;
            openTxt.color = Color.white;
            openTxt.alignment = TextAlignmentOptions.Center;
            openTxt.text = "🎰 GACHA";
            openTxt.fontStyle = FontStyles.Bold;

            TerminalManager tm = Object.FindAnyObjectByType<TerminalManager>();
            if (tm != null && tm.terminalText != null)
            {
                openTxt.font = tm.terminalText.font;
            }

            Button openBtn = openBtnGo.GetComponent<Button>();
            openBtn.onClick.AddListener(gameManager.OpenGachaPanel);
            
            if (gameManager.coffeeBtn != null)
            {
                openBtnGo.transform.SetSiblingIndex(gameManager.coffeeBtn.transform.GetSiblingIndex() + 1);
            }
            EditorUtility.SetDirty(gameManager);
        }
    }
}
// Trigger comment to force recompilation: 2026-06-28T23:44:00
