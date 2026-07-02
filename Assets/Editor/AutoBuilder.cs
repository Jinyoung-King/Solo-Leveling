using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

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

        // 0. 어플 아이콘 세팅
        Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/app_icon.png");
        if (iconTexture != null)
        {
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new Texture2D[] { iconTexture });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new Texture2D[] { iconTexture });
            Debug.Log("[AutoBuilder] App Icon successfully assigned.");
        }
        else
        {
            Debug.LogWarning("[AutoBuilder] Assets/app_icon.png not found or failed to load.");
        }

        // 0-2. 한글/이모지 글리프를 폰트 아틀라스에 미리 구워넣어 두부문자(□) 방지
        BakeFontGlyphs();

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

    // 프로젝트의 모든 .cs 소스에 등장하는 한글/기호/이모지 글자를 폰트 아틀라스에 정적으로 구워넣습니다.
    // Pretendard SDF는 기본적으로 런타임 동적 생성(Dynamic)이고 ClearDynamicDataOnBuild=true 라서
    // 빌드 시 글리프가 비워지고, 안드로이드 런타임 생성이 실패하면 한글이 전부 □(두부문자)가 됩니다.
    // 실제 사용 글자를 미리 베이크하고 clear-on-build 를 끄면 런타임 생성 없이도 확실히 렌더링됩니다.
    public static void BakeFontGlyphs()
    {
        try
        {
            HashSet<char> hangul = new HashSet<char>();
            HashSet<int> seenSymbol = new HashSet<int>();
            System.Text.StringBuilder symbolSb = new System.Text.StringBuilder();

            string assetsDir = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            string[] csFiles = Directory.GetFiles(assetsDir, "*.cs", SearchOption.AllDirectories);
            foreach (string file in csFiles)
            {
                string content;
                try { content = File.ReadAllText(file); } catch { continue; }

                // 서로게이트 페어(이모지) 대응을 위해 텍스트 요소 단위로 순회
                System.Globalization.TextElementEnumerator te = System.Globalization.StringInfo.GetTextElementEnumerator(content);
                while (te.MoveNext())
                {
                    string el = te.GetTextElement();
                    int cp = char.ConvertToUtf32(el, 0);
                    if ((cp >= 0xAC00 && cp <= 0xD7A3) || (cp >= 0x1100 && cp <= 0x11FF) || (cp >= 0x3130 && cp <= 0x318F))
                    {
                        hangul.Add(el[0]); // BMP 한글
                    }
                    else if (cp >= 0x2190) // 화살표/기호/이모지 등
                    {
                        if (seenSymbol.Add(cp)) symbolSb.Append(el);
                    }
                }
            }

            // ASCII 인쇄 가능 문자 + 한글 + 자주 쓰는 한글 문장부호를 Pretendard 에 베이크
            System.Text.StringBuilder krSb = new System.Text.StringBuilder();
            for (int c = 0x20; c < 0x7F; c++) krSb.Append((char)c);
            foreach (char c in hangul) krSb.Append(c);
            foreach (char c in "…·※★☆♥♡◆■□▶◀") krSb.Append(c);

            BakeCharactersInto("Assets/TextMesh Pro/Fonts/Pretendard-Regular SDF.asset", krSb.ToString());
            if (symbolSb.Length > 0)
            {
                BakeCharactersInto("Assets/TextMesh Pro/Fonts/seguiemj SDF.asset", symbolSb.ToString());
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AutoBuilder] Font baking complete. Hangul={hangul.Count}, Symbols/Emoji={seenSymbol.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AutoBuilder] Font baking failed: " + e.Message + "\n" + e.StackTrace);
        }
    }

    private static void BakeCharactersInto(string assetPath, string characters)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (font == null)
        {
            Debug.LogWarning("[AutoBuilder] Font asset not found: " + assetPath);
            return;
        }

        // 멀티 아틀라스 허용(글리프가 많아도 여러 텍스처로 확장) + 빌드 시 데이터 유지
        SerializedObject so = new SerializedObject(font);
        SerializedProperty multi = so.FindProperty("m_IsMultiAtlasTexturesEnabled");
        if (multi != null) multi.boolValue = true;
        SerializedProperty clear = so.FindProperty("m_ClearDynamicDataOnBuild");
        if (clear != null) clear.boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        string missing;
        bool ok = font.TryAddCharacters(characters, out missing);
        int missingCount = string.IsNullOrEmpty(missing) ? 0 : missing.Length;
        Debug.Log($"[AutoBuilder] Baked into {Path.GetFileName(assetPath)}: ok={ok}, missing={missingCount}");

        EditorUtility.SetDirty(font);
        if (font.atlasTextures != null)
        {
            foreach (var tex in font.atlasTextures)
            {
                if (tex != null) EditorUtility.SetDirty(tex);
            }
        }
        if (font.material != null) EditorUtility.SetDirty(font.material);
    }

    public static void SetupGachaUI(GameManager gameManager)
    {
        if (gameManager == null) return;

        // GameManager is a scene-root object (not under the Canvas), so GetComponentInParent<Canvas>()
        // returns null and the whole gacha UI silently fails to be created. Find the Canvas in the scene.
        Canvas canvas = gameManager.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // 이전 빌드에서 생성된 가차 UI를 제거하고 항상 최신 소스 문자열로 재생성 (이모지 제거 등 반영)
        List<GameObject> stale = new List<GameObject>();
        foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && (t.name == "Panel_Gacha" || t.name == "Btn_Open_Gacha"))
            {
                stale.Add(t.gameObject);
            }
        }
        foreach (var go in stale) { if (go != null) Object.DestroyImmediate(go); }
        gameManager.gachaPanel = null;

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
            UITheme.Panel(bgImg, UITheme.PanelBg);

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
            titleText.text = "★ <b>HUNTER EQUIPMENT GACHA</b> ★";

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
            resultText.text = "Mana를 소모하여\n헌터 무기 및 장비를 뽑으세요!\n\n<size=80%>(1회 50,000 Mana -> 뽑을 때마다 상승)</size>";
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

            UITheme.StyleButton(drawBtnGo.GetComponent<Button>(), UITheme.Gold);

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
            drawBtnText.text = "★ 1회 뽑기\nCost: 50.0K Mana";
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

            UITheme.StyleButton(closeBtnGo.GetComponent<Button>(), UITheme.PanelSoft);

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

            UITheme.StyleButton(openBtnGo.GetComponent<Button>(), UITheme.Gold);

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
            openTxt.text = "★ GACHA";
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
